using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using System.Collections.Generic;

public class LSEPlayableBridge : MonoBehaviour
{
    [Header("Configuración de Animación")]
    public Animator animator;
    [Range(0.1f, 2.0f)] public float velocidadGlobal = 1f;
    public float segundosTransicion = 0.5f;

    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private Coroutine secuenciaActual;
    private AnimationClip idleClip;

    private Playable slotPlayable0;
    private Playable slotPlayable1;

    void Awake()
    {
        #if !UNITY_EDITOR && UNITY_WEBGL
            WebGLInput.captureAllKeyboardInput = false;
        #endif
        
        animator.applyRootMotion = true;

        graph = PlayableGraph.Create("LSE_SequenceGraph");
        var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        mixer = AnimationMixerPlayable.Create(graph, 2);
        output.SetSourcePlayable(mixer);
        graph.Play();

        secuenciaActual = StartCoroutine(CargarIdle());
    }

    // idle
    IEnumerator CargarIdle()
    {
        if (idleClip == null)
        {
            var handle = Addressables.LoadAssetAsync<AnimationClip>("idle_anim");
            yield return handle;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[LSE] No se pudo cargar idle_anim");
                yield break;
            }
            idleClip = handle.Result;
        }

        while (true)
        {
            ConectarEnSlot0(AnimationClipPlayable.Create(graph, idleClip));
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);

            yield return new WaitForSeconds(idleClip.length / velocidadGlobal);
        }
    }

    public void ReproducirGlosas(string secuenciaCSV)
    {
        Debug.Log("[LSE] ReproducirGlosas recibido: " + secuenciaCSV);

        if (secuenciaActual != null)
            StopCoroutine(secuenciaActual);

        secuenciaActual = StartCoroutine(CargarYReproducir(secuenciaCSV));
    }


    IEnumerator CargarYReproducir(string listaIDs)
    {
        // si idleClip aún no cargó, esperamos a que esté disponible.
        while (idleClip == null)
            yield return null;

        string[] ids = listaIDs.Split(',');
        var clipsCargados = new List<AnimationClip>();

        foreach (string id in ids)
        {
            string idLimpio = id.Trim();
            if (string.IsNullOrEmpty(idLimpio)) continue;

            var handle = Addressables.LoadAssetAsync<AnimationClip>(idLimpio);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                clipsCargados.Add(handle.Result);
            else
                Debug.LogWarning("[LSE] Clip no encontrado: " + idLimpio);
        }

        if (clipsCargados.Count == 0)
        {
            Debug.LogWarning("[LSE] Sin clips. Volviendo a idle.");
            secuenciaActual = StartCoroutine(CargarIdle());
            yield break;
        }

        // Idle → primer signo
        yield return BridgeToNext(idleClip, clipsCargados[0]);

        // Signos con transiciones entre ellos
        for (int i = 0; i < clipsCargados.Count; i++)
        {
            yield return PlayClipNormal(clipsCargados[i]);

            if (i < clipsCargados.Count - 1)
                yield return BridgeToNext(clipsCargados[i], clipsCargados[i + 1]);
        }

        // Último signo -> idle
        yield return BridgeToNext(clipsCargados[clipsCargados.Count - 1], idleClip);

        // Retoma el loop de idle
        secuenciaActual = StartCoroutine(CargarIdle());
    }

    // Reproducción de clip completo

    IEnumerator PlayClipNormal(AnimationClip clip)
    {
        var playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetSpeed(velocidadGlobal);
        playable.SetApplyFootIK(false);

        ConectarEnSlot0(playable);
        mixer.SetInputWeight(0, 1f);
        mixer.SetInputWeight(1, 0f);

        yield return new WaitForSeconds(clip.length / velocidadGlobal);
    }

    // Transición entre clips

    IEnumerator BridgeToNext(AnimationClip from, AnimationClip to)
    {
        float fixedY = animator.transform.position.y;

        // Pose de salida: último frame de 'from'
        var poseA = AnimationClipPlayable.Create(graph, from);
        poseA.SetTime(from.length);
        poseA.SetSpeed(0f);
        poseA.SetApplyFootIK(false);
        ConectarEnSlot0(poseA);

        // Pose de entrada: primer frame de 'to'
        var poseB = AnimationClipPlayable.Create(graph, to);
        poseB.SetTime(0f);
        poseB.SetSpeed(0f);
        poseB.SetApplyFootIK(false);
        ConectarEnSlot1(poseB);

        float t = 0f;
        while (t < segundosTransicion)
        {
            t += Time.deltaTime;
            float peso = Mathf.SmoothStep(0f, 1f, t / segundosTransicion);
            mixer.SetInputWeight(0, 1f - peso);
            mixer.SetInputWeight(1, peso);

            // Evitar deriva vertical causada por Root Motion durante la transición
            Vector3 pos = animator.transform.position;
            pos.y = fixedY;
            animator.transform.position = pos;

            yield return null;
        }

        mixer.SetInputWeight(0, 0f);
        mixer.SetInputWeight(1, 1f);
    }

    private void ConectarEnSlot0(Playable nuevo)
    {
        if (slotPlayable0.IsValid())
        {
            graph.Disconnect(mixer, 0);
            slotPlayable0.Destroy();
        }
        graph.Connect(nuevo, 0, mixer, 0);
        slotPlayable0 = nuevo;
    }

    private void ConectarEnSlot1(Playable nuevo)
    {
        if (slotPlayable1.IsValid())
        {
            graph.Disconnect(mixer, 1);
            slotPlayable1.Destroy();
        }
        graph.Connect(nuevo, 0, mixer, 1);
        slotPlayable1 = nuevo;
    }

    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}