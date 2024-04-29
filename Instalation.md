# Manual d'Usuari d'OCPP.Core: Construcció i Instal·lació

OCPP.Core consisteix en tres projectes:
1. 🖥️ **OCPP.Core.Server**
2. 🛠️ **OCPP.Core.Management**
3. 💾 **OCPP.Core.Database**

## OCPP.Core.Server
El "Server" és l'aplicació web amb la qual les estacions de càrrega es comuniquen. Entén el protocol OCP i té una petita API REST per a la interfície de gestió.

## OCPP.Core.Management
El "Management" és la interfície d'usuari web que es pot obrir al navegador. Podeu gestionar les estacions de càrrega i els tokens RFID aquí. També podeu veure i descarregar les llistes de transaccions de càrrega.

## OCPP.Core.Database
La "Database" és utilitzada pels altres dos projectes. Conté el codi necessari per llegir i escriure dades a la base de dades.

### Base de Dades
El projecte inclou plantilles per a SQL Server i SQLite:
- **SQL Server (SQL-Express):** Utilitzeu l'script a la carpeta 'SQL-Server' per crear una nova base de dades. Configureu el vostre compte (IIS => AppPool) per llegir i escriure dades.
- **SQLite:** La carpeta 'SQLite' conté un fitxer de base de dades buit llest per utilitzar. O bé, feu servir l'script SQL a la mateixa carpeta. El principal script en ambdues carpetes sempre conté la última versió per a una base de dades completa. Si esteu actualitzant des de versions anteriors, hi ha scripts d'actualització dedicats.

### Servidor Web
El servidor OCPP i la interfície d'usuari web són webs/servidors independents i ambdós necessiten informació de connexió a la base de dades. La interfície d'usuari web necessita la URL del servidor OCPP per obtenir informació d'estat i realitzar algunes accions. El fitxer de configuració de la interfície d'usuari web conté els usuaris i contrasenyes.

### Configuració del OCPP.Core.Server
Editeu el fitxer `appsettings.json` i configureu l'entrada 'SqlServer':

![Configuració del OCPP.Core.Server](/images/config_server.png)

### Configuració del OCPP.Core.Management
La interfície de gestió necessita la URL del servidor OCPP per a la comunicació interna. Per assegurar aquesta API, podeu configurar claus d'API (= contrasenyes idèntiques) en ambdós costats.

![Configuració del OCPP.Core.Management](/images/config_management.png)

## Execució
Execució amb Kestrel (servidor web simple):
Els executables per als dos projectes web (Server i Management) es troben al sortida del compilador a la carpeta “OCPP16\OCPP.Core.Server\bin\Debug\net8.0” i “OCPP16\OCPP.Core.Management\bin\Debug\net8.0”.
- `OCPP.Core.Server.exe` i `OCPP.Core.Management.exe`
Podeu iniciar aquests executables. Això iniciarà les aplicacions amb el servidor web Kestrel. Veureu una consola de comandes on es mostraran les URL actives i tota la sortida de registre.
