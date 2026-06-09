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
- **Sensor de Red Inteligente (Escudo de Red):** Captura sockets de conexión entrante y saliente en tiempo real (`sshd`, `chrome`, etc.). Cuenta con un **Filtro Anti-Spam** integrado que mitiga inundaciones de alertas repetidas en la terminal bajo un esquema de enfriamiento activo y es capaz de registrar escaneos de puertos externos por IPs sospechosas en entornos públicos.
- **Notificaciones:** Integración asíncrona con Telegram Bot API para el despacho de alertas críticas e instantáneas.
- **Auditoría Forense (Logging):** - **Humana:** Logs estructurados y enriquecidos en consola con marcas de tiempo legibles y tags de seguridad (`[Filtro de Red]`, `[Auditoría]`).
    - **Máquina:** Logs persistidos en formato **JSON (CLEF)** con escritura directa a disco sin almacenamiento previo en búfer (`buffered: false`) para garantizar la preservación inmediata de evidencia.
- **Calidad de Software:** Suite de pruebas unitarias funcionales con **xUnit** y **Moq** (4/4 tests exitosos en el entorno de desarrollo).

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
      "/Logs/",
      "/tmp/",
      "/.local/state/",
      "wireplumber",
      ".log",
      ".tmp",
      ".git",
      "swp",
      ".lock",
      "CiberGuard_Audit",
      ".sqlite",
      ".sqlite-wal",
      ".sqlite-shm",
      ".new"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "EscudoRed": {
      "VentanaTiempoSegundos": 60,
      "MaxIntentosPermitidos": 3,
      "MinutosEnfriamientoAlerta": 5
    }
  
}
```
#### 3. Configuración del Secreto Criptográfico
Para garantizar el blindaje de integridad de los registros mediante `HMAC-SHA256` y evitar la manipulación o borrado silencioso de huellas por parte de un atacante, es obligatorio configurar la clave secreta de firmado:

```bash
dotnet user-secrets set "Security:LogKey" "Clave"
```

### 🧠 Motor de Correlación Forense Avanzada (Nuevo)
El sistema ya no solo lee cambios en los archivos de historial (`.zsh_history` / `.bash_history`), sino que implementa una heurística de desempate en tiempo real basada en el estado de los procesos del sistema:

* **Detección por Inactividad (*Idle Time*):** Mediante el análisis de sockets activos (`ss`) y el estado de terminales (`who -u`), el sistema discrimina con precisión si un comando provino de la **Consola Física / Local TTY** o de una sesión interactiva remota **Remoto (IP:Port vía SSH)** (ej. desde *Termux*), incluso si ambos operadores están ejecutando comandos de forma simultánea.
* **Regla de Oro de Cierre:** Interceptación inmediata de señales de destrucción de shell (`exit`/`logout`) asignándolas de manera prioritaria al canal remoto de origen antes de la liberación de sockets TCP.