# RamonaLSE

RamonaLSE es un Trabajo de Fin de Grado que consiste en un pipeline completo de 
Producción de Lengua de Signos (SLP) capaz de traducir cualquier frase a Lengua 
de Signos Española (LSE) y reproducirla visualmente a través de un avatar 3D, 
tomando como entrada tanto audio como texto.

Este proyecto está adaptado para el sistema operativo de macOS con Apple Silicon. En un futuro se analizará la adaptación a otros sistemas operativos como Windows o Linux.

## Pipeline

El proyecto contempla un flujo compuesto por las siguientes etapas:

- **Transcripción de audio** con [Whisper.cpp-large-v3-turbo](https://huggingface.co/ggerganov/whisper.cpp).
- **Traducción a glosas LSE** a partir del corpus sintético de [ruLSE](https://github.com/celiabotlop/LSEGloss2SpanishText).
- **Extracción de landmarks** de vídeos de signantes del corpus DILSE con [MediaPipe Hand Landmarker](https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker) y [MediaPipe Pose Landmarker](https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker).
- **Síntesis de animaciones** a partir de los landmarks extraídos, generadas en [Blender](https://www.blender.org) e integradas en [Unity](https://unity.com/es).
- **Interfaz interactiva**, con [Flask](https://flask.palletsprojects.com/en/stable/) como herramienta de comunicación entre backend y frontend, que integra el pipeline completo permitiendo obtener la representación en LSE de cualquier frase en tiempo real.

## Estructura del repositorio

```
├── animaciones/             # FBX de las animaciones sintetizadas
├── animaciones_json/        # Extracción de landmarks y filtrado One Euro
├── BuildWebRamona/          # Build WebGL del proyecto de Unity
├── modelo/                  # Servidor Flask — transcripción, traducción y secuenciación de animaciones. Captura de landmarks
├── pruebas/                 # Pruebas realizadas y resultados
├── videos_marcados/         # Vídeos procesados de DILSE junto con los resultados de MediaPipe
└── /                        # Proyecto Unity y Blender — avatar 3D
```

## Tecnologías

| Componente | Tecnología |
|---|---|
| Transcripción de voz | Whisper.cpp |
| Traducción a glosas | Stanza + ruLSE |
| Captura de movimiento | MediaPipe |
| Generación de animaciones | Blender |
| Avatar e interfaz | Unity (WebGL) |
| Backend | Flask |


## Requisitos e Instalación

### 1. Requisitos previos
Antes de la instalación, es necesario contar con lo siguiente:
   - Python >= 3.10, < 3.12

Aunque no es obligatorio, es recomendable disponer de macOS con Apple Silicon.

### 2. Instalación de dependencias
Para instalar las dependencias de RamonaLSE, ejecuta el siguiente comando.

```bash
pip install -r requirements.txt
```

### 3. Whisper.cpp (Apple Silicon)
1. **Descargar el modelo:**
   - Ve al repositorio de Hugging Face de Whisper.cpp: [ggerganov/whisper.cpp](https://huggingface.co/ggerganov/whisper.cpp/tree/main)
   - Descarga el modelo `.bin` que desees (por ejemplo, `ggml-large-v3-turbo.bin`). En el caso del macOS, se localizará como un directorio `whisper.cpp` que contendrá el modelo CoreML.

2. Compila Whisper.cpp con CoreML con el siguiente comando en terminal:

```bash
CMAKE_ARGS="-DWHISPER_COREML=1 -DCMAKE_POLICY_VERSION_MINIMUM=3.5" pip install whisper-cpp-python
```

Si no se dispone de macOS la instalación de Whisper.cpp será distinta, pero este cambio no afecta al flujo de trabajo propuesto.

### Nota: Modelos de MediaPipe
MediaPipe no es necesario para ejecutar RamonaLSE en producción, pues los landmarks ya han sido extraídos de los vídeos del corpus DILSE y las animaciones resultantes están integradas y listas para su visualización. No obstante, si se desea regenerar los landmarks desde cero, es necesario descargar los modelos preentrenados oficiales de Google y colocarlos en la carpeta `landmarkers/` dentro del directorio raíz del proyecto:
* [Hand Landmarker Task](https://developers.google.com/mediapipe/solutions/vision/hand_landmarker/index#models) (`hand_landmarker.task`)
* [Pose Landmarker Task](https://developers.google.com/mediapipe/solutions/vision/pose_landmarker/index#models) (`pose_landmarker_full.task`)


## Puesta en marcha

Para ejecutar el pipeline completo de RamonaLSE, es necesario levantar tres servicios simultáneamente (necesitarás tres ventanas de terminal).

### Terminal 1: Servidor de Whisper.cpp
Levanta el servidor local de transcripción de audio escuchando en el puerto 8080:

```bash
cd ruta_hasta_whisper.cpp/whisper.cpp
./build/bin/whisper-server -m models/ggml-large-v3-turbo.bin -l es
```

### Terminal 2: Backend (Servidor Flask)
Levanta la API de Python que gestiona la traducción y la lógica. Se habilitará el puerto 5050:

```bash
cd ruta_hasta_avatar_lse/avatar_lse/modelo
python servidor.py
```

### Terminal 3: Frontend (Unity WebGL)
Levanta un servidor HTTP simple para visualizar el avatar interactivo en el navegador en el puerto 8000:

```bash
cd ruta_hasta_avatar_lse/avatar_lse/BuildWebRamona
python -m http.server 8000
```

Una vez que los tres servicios estén corriendo, abre tu navegador y entra en `http://localhost:8000` para interactuar con RamonaLSE.