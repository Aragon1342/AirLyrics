# ✦ AirLyrics

Overlay flotante e interactivo para Windows desarrollado en **C# (.NET 8 y WPF)** para sincronizar y mostrar las letras de la música que suena en **Spotify** en tiempo real, con soporte para **Modo Fantasma (Click-Through)** estilo *Ghost Chat*.

---

## ✨ Características Principales

- 👻 **Modo Fantasma (Click-Through):** Alterna entre el modo edición (mover/redimensionar) y el modo fantasma mediante llamadas directas a Win32 API (`WS_EX_TRANSPARENT`). La ventana se vuelve 100% invisible en el fondo y deja interactuar con tus juegos, videos o navegador.
- ⌨️ **Atajo Global Personalizable:** Activa o desactiva el modo fantasma con tu combinación de teclas favorita (por defecto `Ctrl + Alt + G`), detectable incluso dentro de videojuegos a pantalla completa.
- 🟢 **Integración Oficial con Spotify (OAuth 2.0 PKCE):** Conexión segura y polling en tiempo real con extrapolación suave de milisegundos (`progress_ms`).
- 🎵 **Sincronización con LRCLIB:** Búsqueda automática y sincronización de letras (.lrc) verso a verso con auto-scroll fluido y resaltado activo.
- 🎨 **Personalización Total:** Selector de paleta de colores predeterminados (incluyendo modo oscuro/negro con sombra adaptativa) y panel con slider de tono arcoíris (Hue) y luminosidad.
- 🔠 **Ajuste de Fuente y Tamaño de Ventana:** Botones `A-` / `A+` y redimensionamiento nativo en todas las direcciones.
- 📦 **Standalone Portable (.EXE):** No requiere instalar runtimes ni dependencias externas.

---

## 📥 Descarga

Descarga el ejecutable listo para usar desde la sección de **[Releases](../../releases/latest)**:
- **`AirLyrics.exe`**: Ejecutable directo sin instalación.
- **`AirLyrics-Windows-x64.zip`**: Versión comprimida.

---

## 🚀 Publicar una nueva versión (GitHub Actions)

Cada vez que quieras compilar y publicar una nueva versión automáticamente en GitHub Releases, solo ejecuta desde tu consola:

```bash
# 1. Guarda tus cambios
git add .
git commit -m "feat: nueva versión de AirLyrics"
git push origin main

# 2. Crea un tag de versión y súbelo
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions compilará automáticamente el proyecto en Windows y creará la Release con los ejecutables adjuntos.

---

## 🛠️ Compilación Local

Si deseas compilar manualmente en tu equipo:
```powershell
# Compilar como ejecutable único independiente
.\Build-Executable.ps1
```
El archivo final se generará en `publish/AirLyrics.exe`.