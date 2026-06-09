# 🗺️ CyberGuardArch: Roadmap de Ingeniería

Este documento detalla las fases de desarrollo, requisitos técnicos y el estado de los componentes del sistema de monitoreo industrial.

## 🏗️ Fase 0: Arquitectura, Seguridad y Contratos
*El objetivo es blindar el sistema y garantizar la escalabilidad multiplataforma mediante desacoplamiento total.*

### 📂 Diseño de Arquitectura Limpia (Clean Architecture)
- [ x ] **Definición de Proyectos (Solución .NET):** 
    - `SentinelArch.Core`: Lógica de negocio e interfaces (Sin dependencias externas).
    - `SentinelArch.Infrastructure`: Implementaciones de bajo nivel (Linux APIs, Telegram SDK).
    - `SentinelArch.Worker`: Servicio de fondo (Host del sistema).
- [ x ] **Diagrama de Componentes:** Mapeo de flujo de eventos: *Hardware -> Kernel -> SentinelArch -> Cifrado -> Notificador.*
- [ x ] **Inyección de Dependencias (DI):** Registro de servicios para intercambio en caliente de módulos (ej. cambiar de `LinuxNetworkMonitor` a `WindowsNetworkMonitor`).

### 🔐 Protocolo de Seguridad e Integridad (Cybersecurity Layer)
- [ x ] **Gestión de Secretos:** Implementación de `UserSecrets` para desarrollo y variables de entorno para producción (Prohibido Hardcoding de Tokens).
- [ x ] **Validación de Integridad de Logs:** Diseño de una firma simple para los logs, evitando que un atacante borre huellas de archivos modificados.
- [ x ] **Criptografia:** Implementación de `Secrets` para la criptografia de los logs.
- [ ] **Principio de Menor Privilegio:** Definición de permisos mínimos necesarios para que el binario de .NET acceda a `nmcli` y al sistema de archivos sin ser `root` innecesariamente.

### 📜 Definición de Contratos (Interfaces Core)
- [ x ] **`IMonitorService`:** Interfaz para sensores de Red y Archivos. Debe soportar cancelación asíncrona (`CancellationToken`).
- [ x ] **`INotificationService`:** Interfaz para servicios de alerta (Telegram, Desktop Notifier).
- [ x ] **`IFileSystemWatcher`:** Abstracción para evitar dependencia directa de `System.IO.FileSystemWatcher` y permitir testing con mocks.

### 🔧 Gestión de Configuración y DevOps (Git & Workflow)
- [ x ] **Estructura de Repositorio Profesional:**
- [ x ] Creación de `.gitignore` optimizado para .NET.
- [ x ] **Estrategia de Versionamiento:** Establecer ramas `main` y `develop` con GitFlow simplificado.

---

## 🛰️ Fase 1: Infraestructura de Notificaciones (Telegram Bot)
*Establecer el canal de salida seguro antes de procesar datos.*

### [ x ] Integración con Telegram API
- [ x ] Creación de Bot mediante BotFather (Documentar proceso).
- [ x ] Implementación de `TelegramService` en C#.
- [ x ] **Pruebas de Conectividad:** Script de validación mediante `curl` y prueba unitaria en .NET.
- [ x ] Envio de mensaje de bienvenida  desde el servicio notification
---

## 🐧 Fase 2: Módulos de Monitoreo (Enfoque Arch Linux)
*Implementación de los sensores de sistema específicos para Linux.*

### [x] Sensor de Red (Network Sensor)
- [x] **Captura de Sockets Activos:** Identificación automatizada de conexiones entrantes y salientes mediante parsing nativo de utilidades de red (`ss`).
- [x] **Enriquecimiento de Metadatos:** Extracción en tiempo real de IP origen, puerto local, protocolo de transporte (`tcp`) y mapeo exacto del proceso propietario (`sshd`, `chrome`, etc.).
- [x] **Filtro Anti-Spam Operativo:** Mecanismo de caché en memoria (`Dictionary`) con ventana de enfriamiento de 5 minutos para mitigar inundaciones por conexiones repetitivas.
- [x] **Detección de Intrusiones:** Identificación de barridos de puertos externos o intentos fallidos de autenticación por IP de bots automatizados de internet en el puerto SSH (22).

### [x] Sensor de Sistema de Archivos & Auditoría de Terminales (Watcher)
- [x] **Monitoreo Recursivo de Rutas Críticas:** Vigilancia atómica de eventos `CREADO`, `MODIFICADO` y `ELIMINADO` en `/etc`, `/bin`, `/usr/bin`, `/root` y `/home`.
- [x] **Sistema Dinámico de Exclusiones:** Filtrado de ruido masivo en segundo plano generado por el entorno gráfico (KDE/Dolphin, buffers `.tmp`, archivos `.lock`, directorios `.cache` y mutaciones de audio de Wireplumber).
- [x] **Motor de Correlación Forense de Comandos (Hito del Día):**
  - [x] Raspado asíncrono y en caliente de mutaciones en búferes de historiales de shell (`.zsh_history` / `.bash_history`).
  - [x] **Algoritmo de Desempate Estricto por Inactividad (*Idle Time*):** Resolución analítica mediante pseudo-terminales virtuales (`who -u`). El motor discrimina con precisión milimétrica si un comando fue ejecutado físicamente en la máquina (`Consola Física / Local TTY`) o de forma remota (`Remoto [IP:Puerto vía SSH]` desde Termux), mitigando el solapamiento o inversión de eventos cuando ambos entornos escriben en paralelo.
  - [x] **Regla de Interceptación de Salidas:** Priorización de señales de destrucción de shell (`exit`/`logout`) asignándolas instantáneamente al operador remoto antes del cierre del socket TCP.
### [ ] Interfaz de Usuario Local
- [ ] Alertas visuales usando `notify-send` / `Zenity`.

---

## 🛡️ Fase 3: Persistencia y Calidad (Forense)
- [x] **Logging Industrial:** Implementación de logs estructurados en JSON para fácil parsing.
- [x] **Blindaje Criptográfico Integrado:** Firmado digital de logs en tiempo real combinando marcas de tiempo, niveles de alerta y rutas mediante cifrado simétrico `HMAC-SHA256` acoplado al módulo de secretos (`Security:LogKey`).
- [ ] **Daemonization:** Creación del archivo de unidad de `systemd` para que SentinelArch inicie con el sistema.
- [ ] **Suite de Pruebas:** Cobertura de tests unitarios (XUnit) mínima del 80%.
   - [x] **Lógica de Negocio:** Test de `CyberGuardArchWorker` (Validación de secretos/tokens).
   - [x] **Integración Lógica:** Test de flujo sensor -> notificador (Mocking de eventos).
   - [x] **Cobertura Inicial:** 4/4 Tests exitosos en .NET 10.
   - [x] **Pruebas de LinuxNetworkMonitorService**

---

## 🚀 Fase 4: Abstracción para Escalabilidad (Hacia Windows)
- [ ] Creación de proveedores específicos para Windows (Event Viewer integration).
- [ ] Refactorización de rutas POSIX a rutas agnósticas al SO.