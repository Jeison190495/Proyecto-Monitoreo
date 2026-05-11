# 🛡️ CyberGuardArch
**Sistema de Monitoreo Proactivo e Inteligente para Entornos de Ingeniería.**

CyberGuardArch es un servicio de monitoreo industrial diseñado para la supervisión en tiempo real de cambios críticos en red y sistema de archivos. Construido sobre **.NET 10** y optimizado inicialmente para **Arch Linux**, utiliza una arquitectura desacoplada que permite la escalabilidad hacia Windows y plataformas Cloud.

## 🚀 Propósito
Proporcionar una capa de seguridad y auditoría forense para desarrolladores, notificando de forma inmediata via **Telegram** sobre:
- Conexiones y cambios de red sospechosos.
- Manipulación de archivos en directorios críticos.
- Alertas visuales nativas en el entorno de escritorio.

## 🛠️ Stack Tecnológico
- **Lenguaje:** C# (.NET)
- **SO Primario:** Arch Linux (Kernel 6+)
- **Arquitectura:** Clean Architecture + Patrón Observer.
- **Notificaciones:** Telegram Bot API + Zenity/Notify-send.
- **Versionamiento:** Git con flujo de trabajo semántico.

## 🏗️ Arquitectura (Fase 0)
El proyecto se divide en tres capas fundamentales:
1. **Core:** Contiene las interfaces y la lógica pura.
2. **Infrastructure:** Implementaciones de bajo nivel (sensores Linux y API de Telegram).
3. **Worker:** El daemon que orquesta el servicio en segundo plano.

## 🛠️ Configuración del Bot de Telegram

Para que el sistema funcione, necesitas configurar un bot con **BotFather**:

1. Crea un bot y obtén el `Token`.
2. Obtén tu `ChatId` (puedes usar el comando `curl` mencionado en la documentación técnica).
3. Configura el nombre del bot en el sistema.

### 🔐 Gestión de Secretos (Local)
Para evitar subir credenciales a GitHub, usamos **.NET User Secrets**. Ejecuta los siguientes comandos en la carpeta `src/CyberGuardArch.Worker`:
```bash
dotnet user-secrets init
dotnet user-secrets set "Telegram:Token" "TU_TOKEN_AQUI"
dotnet user-secrets set "Telegram:ChatId" "TU_ID_AQUI"
dotnet user-secrets set "Telegram:NameBot" "NOMBRE_DE_TU_BOT"
```

## 📈 Estado Actual del Proyecto
Actualmente, el sistema ha completado su **Base Forense (Fase 0)** y su **Primer Módulo de Monitoreo (Fase 2)**.

### ✅ Funcionalidades Operativas:
- **Monitoreo de Archivos:** Sensor recursivo que detecta `CREADO`, `MODIFICADO` y `ELIMINADO` en tiempo real.
- **Notificaciones:** Integración con Telegram Bot API para alertas instantáneas.
- **Auditoría Forense (Logging):** 
    - **Humana:** Logs limpios en consola con timestamps locales.
    - **Máquina:** Logs estructurados en **JSON (formato CLEF)** para futura integración con Dashboards, garantizando persistencia inmediata (`buffered: false`).
- **Calidad de Software:** Suite de pruebas unitarias con **xUnit** y **Moq** (4/4 tests exitosos).

## 🧪 Pruebas del Sistema
Para validar la integridad de todos los módulos, ejecuta:
```bash
dotnet test
```
### 🔐 Configuración de Auditoría y Secretos (Recomendado)

Para una auditoría de seguridad efectiva en **Arch Linux**, se recomienda monitorear rutas del sistema que manejan privilegios y binarios. Ejecuta los siguientes comandos en la carpeta `src/CyberGuardArch.Worker`:

#### 1. Configuración de Rutas Críticas
Ejecuta estos comandos para establecer los objetivos de vigilancia:
```bash
dotnet user-secrets set "Monitoreo:Rutas:0" "/etc"      # Configuraciones de sistema y passwords
dotnet user-secrets set "Monitoreo:Rutas:1" "/bin"      # Binarios esenciales
dotnet user-secrets set "Monitoreo:Rutas:2" "/usr/bin"  # Aplicaciones de usuario
dotnet user-secrets set "Monitoreo:Rutas:3" "/root"     # Directorio del superusuario
dotnet user-secrets set "Monitoreo:Rutas:4" "/home"     # Datos de usuario (Requiere filtro de ruido)
```
#### 2. Configuración de Excepciones para no tener bucles infinitos
Para evitar bucles infinitos de logs y falsos positivos generados por el entorno de escritorio (KDE/Dolphin, Caches, etc.), debes añadir la sección Exclusiones en tu archivo appsettings.json
Nota: El sistema ignorará cualquier cambio en las rutas que contengan estos patrones, optimizando el consumo de CPU y datos.
```code
{
  "Monitoreo": {
    "Exclusiones": [
      "/.cache/",
      "/.config/",
      "/.local/share/",
      "/.local/state/",
      "wireplumber",
      "CiberGuard_Audit",
      "/Logs/",
      ".log",
      ".tmp",
      ".lock",
      "swp",
      ".git"
    ]
  }
}
```