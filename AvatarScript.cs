using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class LSEPlayableBridge : MonoBehaviour
{
    [Header("Configuración de Animación")]
    public Animator animator;
    [Range(0.1f, 2.0f)] public float velocidadGlobal = 0.9f;
    public float segundosTransicion = 0.15f;

    [Header("Configuración UDP")]
    public int puerto = 9000;

    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private Coroutine secuenciaActual;

    private UdpClient clienteUDP;
    private bool hayMensajePendiente = false;
    private string mensajeRecibido = "";

    void Awake()
    {
        animator.applyRootMotion = true;

        graph = PlayableGraph.Create("LSE_SequenceGraph");
        var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);

        mixer = AnimationMixerPlayable.Create(graph, 2);
        output.SetSourcePlayable(mixer);

        graph.Play();
    }

    void Start()
    {
        try
        {
            clienteUDP = new UdpClient(puerto);
            clienteUDP.BeginReceive(RecibirDatos, null);
            Debug.Log($"Escuchando LSE en puerto {puerto}...");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error UDP: " + e.Message);
        }
    }

    private void RecibirDatos(System.IAsyncResult res)
    {
        IPEndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = clienteUDP.EndReceive(res, ref remoteIp);

        mensajeRecibido = Encoding.UTF8.GetString(data);
        hayMensajePendiente = true;

        clienteUDP.BeginReceive(RecibirDatos, null);
    }

    void Update()
    {
        if (!hayMensajePendiente) return;

        hayMensajePendiente = false;

        if (secuenciaActual != null)
            StopCoroutine(secuenciaActual);

        secuenciaActual = StartCoroutine(CargarYReproducir(mensajeRecibido));
    }

    IEnumerator CargarYReproducir(string listaIDs)
    {
        string[] ids = listaIDs.Split(',');
        List<AnimationClip> clipsCargados = new List<AnimationClip>();

        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;

            var handle = Addressables.LoadAssetAsync<AnimationClip>(id);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                clipsCargados.Add(handle.Result);
        }

        for (int i = 0; i < clipsCargados.Count; i++)
        {
            yield return PlayClipNormal(clipsCargados[i]);

            if (i < clipsCargados.Count - 1)
                yield return BridgeToNext(clipsCargados[i], clipsCargados[i + 1]);
        }
    }

    IEnumerator PlayClipNormal(AnimationClip clip)
    {
        LimpiarInput(0);

        var playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetSpeed(velocidadGlobal);
        playable.SetApplyFootIK(false);

        graph.Connect(playable, 0, mixer, 0);
        mixer.SetInputWeight(0, 1f);
        mixer.SetInputWeight(1, 0f);

        yield return new WaitForSeconds(clip.length / velocidadGlobal);
    }

    IEnumerator BridgeToNext(AnimationClip from, AnimationClip to)
    {
        float fixedY = animator.transform.position.y;

        LimpiarInput(0);
        var poseA = AnimationClipPlayable.Create(graph, from);
        poseA.SetTime(from.length);
        poseA.SetSpeed(0f);
        poseA.SetApplyFootIK(false);
        graph.Connect(poseA, 0, mixer, 0);

        LimpiarInput(1);
        var poseB = AnimationClipPlayable.Create(graph, to);
        poseB.SetTime(0f);
        poseB.SetSpeed(0f);
        poseB.SetApplyFootIK(false);
        graph.Connect(poseB, 0, mixer, 1);

        float t = 0f;
        while (t < segundosTransicion)
        {
            t += Time.deltaTime;
            float peso = Mathf.SmoothStep(0f, 1f, t / segundosTransicion);

            mixer.SetInputWeight(0, 1f - peso);
            mixer.SetInputWeight(1, peso);

            // fijar altura para evitar que el avatar se mueva en el eje Y al cambiar de pose
            // (que no suba y baje al cambiar el centro de gravedad del cuerpo)
            Vector3 pos = animator.transform.position;
            pos.y = fixedY;
            animator.transform.position = pos;

            yield return null;
        }
    }

    private void LimpiarInput(int index)
    {
        if (mixer.GetInput(index).IsValid())
            graph.Disconnect(mixer, index);
    }

    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();

        clienteUDP?.Close();
    }
}