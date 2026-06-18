# RamonaLSE

RamonaLSE es un Trabajo de Fin de Grado que consiste en un pipeline completo de 
Producción de Lengua de Signos (SLP) capaz de traducir cualquier frase a Lengua 
de Signos Española (LSE) y reproducirla visualmente a través de un avatar 3D, 
tomando como entrada tanto audio como texto.

## Pipeline

El proyecto contempla un flujo compuesto por las siguientes etapas:

- **Transcripción de audio** con Whisper.cpp.
- **Traducción a glosas LSE** a partir del corpus sintético de ruLSE.
- **Extracción de landmarks** de vídeos de signantes del corpus DILSE con MediaPipe.
- **Síntesis de animaciones** a partir de los landmarks extraídos, generadas en Blender e integradas en Unity.
- **Interfaz interactiva** que integra el pipeline completo, permitiendo obtener la representación en LSE de cualquier frase en tiempo real.

## Estructura del repositorio

```
├── animaciones/             # FBX de las animaciones sintetizadas
├── animaciones_json/        # Extracción de landmarks y filtrado One Euro
├── BuildWebRamona/          # Build WebGL del proyecto de Unity
├── landmarkers/             # Tareas Hands y Pose de MediaPipe
├── modelo/                  # Servidor Flask — transcripción, traducción y secuenciación de animaciones. Captura de landmarks con MediaPipe.
├── pruebas/                 # Resultados de las pruebas realizadas de captura de landmarks
├── videos_marcados/         # Vídeos procesados de DILSE junto con los resultados de MediaPipe
├── whisper.cpp/             # Modelo de Whisper.cpp
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