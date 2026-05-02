# 🗺️ CyberGuardArch: Roadmap de Ingeniería

Este documento detalla las fases de desarrollo, requisitos técnicos y el estado de los componentes del sistema de monitoreo industrial.

## 🏗️ Fase 0: Arquitectura, Seguridad y Contratos
*El objetivo es blindar el sistema y garantizar la escalabilidad multiplataforma mediante desacoplamiento total.*

### 📂 Diseño de Arquitectura Limpia (Clean Architecture)
- [ ] **Definición de Proyectos (Solución .NET):** 
    - `SentinelArch.Core`: Lógica de negocio e interfaces (Sin dependencias externas).
    - `SentinelArch.Infrastructure`: Implementaciones de bajo nivel (Linux APIs, Telegram SDK).
    - `SentinelArch.Worker`: Servicio de fondo (Host del sistema).
- [ ] **Diagrama de Componentes:** Mapeo de flujo de eventos: *Hardware -> Kernel -> SentinelArch -> Cifrado -> Notificador.*
- [ ] **Inyección de Dependencias (DI):** Registro de servicios para intercambio en caliente de módulos (ej. cambiar de `LinuxNetworkMonitor` a `WindowsNetworkMonitor`).

### 🔐 Protocolo de Seguridad e Integridad (Cybersecurity Layer)
- [ ] **Gestión de Secretos:** Implementación de `UserSecrets` para desarrollo y variables de entorno para producción (Prohibido Hardcoding de Tokens).
- [ ] **Validación de Integridad de Logs:** Diseño de una firma simple para los logs, evitando que un atacante borre huellas de archivos modificados.
- [ ] **Principio de Menor Privilegio:** Definición de permisos mínimos necesarios para que el binario de .NET acceda a `nmcli` y al sistema de archivos sin ser `root` innecesariamente.

### 📜 Definición de Contratos (Interfaces Core)
- [ ] **`IMonitorService`:** Interfaz para sensores de Red y Archivos. Debe soportar cancelación asíncrona (`CancellationToken`).
- [ ] **`INotificationService`:** Interfaz para servicios de alerta (Telegram, Desktop Notifier).
- [ ] **`IFileSystemWatcher`:** Abstracción para evitar dependencia directa de `System.IO.FileSystemWatcher` y permitir testing con mocks.

### 🔧 Gestión de Configuración y DevOps (Git & Workflow)
- [ ] **Estructura de Repositorio Profesional:**
    - [ ] Creación de `.gitignore` optimizado para .NET.
    - [ ] Configuración de archivo `.editorconfig`.
- [ ] **Estrategia de Versionamiento:** Establecer ramas `main` y `develop` con GitFlow simplificado.

---

## 🛰️ Fase 1: Infraestructura de Notificaciones (Telegram Bot)
*Establecer el canal de salida seguro antes de procesar datos.*

### [ ] Integración con Telegram API
- [ ] Creación de Bot mediante BotFather (Documentar proceso).
- [ ] Implementación de `TelegramService` en C#.
- [ ] **Pruebas de Conectividad:** Script de validación mediante `curl` y prueba unitaria en .NET.

---

## 🐧 Fase 2: Módulos de Monitoreo (Enfoque Arch Linux)
*Implementación de los sensores de sistema específicos para Linux.*

### [ ] Sensor de Red (Network Sensor)
- [ ] Captura de eventos mediante `nmcli` o lectura de `/proc/net/`.
- [ ] Extracción de metadatos: IP, SSID, Marca de tiempo.
### [ ] Sensor de Sistema de Archivos (Watcher)
- [ ] Monitoreo de directorios clave mediante `FileSystemWatcher` optimizado para Linux.
- [ ] Detección de: Creación, Modificación, Eliminación.
### [ ] Interfaz de Usuario Local
- [ ] Alertas visuales usando `notify-send` / `Zenity`.

---

## 🛡️ Fase 3: Persistencia y Calidad (Forense)
- [ ] **Logging Industrial:** Implementación de logs estructurados en JSON para fácil parsing.
- [ ] **Daemonization:** Creación del archivo de unidad de `systemd` para que SentinelArch inicie con el sistema.
- [ ] **Suite de Pruebas:** Cobertura de tests unitarios (XUnit) mínima del 80%.

---

## 🚀 Fase 4: Abstracción para Escalabilidad (Hacia Windows)
- [ ] Creación de proveedores específicos para Windows (Event Viewer integration).
- [ ] Refactorización de rutas POSIX a rutas agnósticas al SO.