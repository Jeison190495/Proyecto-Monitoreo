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

## 📈 Estado Actual del Desarrollo
Actualmente el proyecto se encuentra en la **Fase 2**. 

### ✅ Funcionalidades Operativas:
- **Notificaciones:** Integración completa con Telegram. El sistema notifica el inicio de sesión y la máquina origen.
- **Monitoreo de Archivos:** Sensor recursivo funcional. Detecta y reporta en tiempo real:
    - `CREADO`: Nuevos archivos o carpetas.
    - `MODIFICADO`: Cambios en contenido o metadatos.
    - `ELIMINADO`: Borrado de archivos o directorios (incluyendo recursión).
- **Calidad:** Suite de pruebas unitarias con **xUnit** y **Moq** integrada en el flujo de trabajo.

### 🧪 Ejecución de Pruebas
Para validar la integridad del sistema en tu entorno de desarrollo, ejecuta:
```bash
dotnet test
```