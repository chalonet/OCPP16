# Manual per afegir una nova empresa amb carregadors i tags

## Pas 1: Iniciar sessió 

El primer pas és iniciar sessió amb l'usuari "superadmin" i la contrasenya "1234". Un cop dins, el primer que farem serà crear l'usuari administrador per a la nova empresa i, seguidament, assignar-li l'empresa.

## Pas 2: Creació de l'usuari administrador 

Accediu a la pàgina d'usuaris on veureu la llista de tots els usuaris: administradors, usuaris normals i superadministradors. Per crear un nou usuari, cliqueu al botó "New" i ompliu el formulari amb les dades de l'administrador. Assegureu-vos d'introduir el nom d'usuari, correu electrònic, contrasenya i seleccionar el rol "Administrator".

![Creació de l'usuari administrador](/images/1.png)
![Creació de l'usuari administrador](/images/2.png)

## Pas 3: Creació de la nova empresa 

Per crear la nova empresa, aneu al menú desplegable i seleccioneu "Companies". Això us portarà a la pàgina del llistat de totes les empreses existents. Cliqueu al botó "New" per crear la nova empresa i ompliu el formulari amb les dades de l'empresa, seleccionant l'administrador creat anteriorment.

![Creació de la nova empresa](/images/3.png)
![Creació de la nova empresa](/images/4.png)

## Pas 4: Gestió de l'administrador i el sistema 

Un cop creat l'administrador i la nova empresa, podeu tancar la sessió del superadmin i iniciar sessió amb l'usuari administrador que heu creat.

![Gestió de l'administrador i el sistema](/images/5.png)

## Pas 5: Creació de punts de càrrega i tags 

Per començar a crear els punts de càrrega, accediu al desplegable i seleccioneu "Charge Points". Això us portarà al llistat dels punts de càrrega associats a la nova empresa. Cliqueu a "New" i ompliu el formulari per crear un punt de càrrega.
Segueix el mateix procés per crear un tag d'exemple: accediu al desplegable de "RFID-Tags", on veureu el llistat de tags de la nova empresa. Cliqueu a "New" i ompliu el formulari.

![Creació de punts de càrrega i tags](/images/6.png)
![Creació de punts de càrrega i tags](/images/7.png)
![Creació de punts de càrrega i tags](/images/8.png)
![Creació de punts de càrrega i tags](/images/9.png)

## Pas 6: Assignació de temps de càrrega 

Per assignar un temps de càrrega, aneu a "Edit Charging Time", assigneu un temps de càrrega i cliqueu a "Add Time". Això enviarà un correu des de l'administrador al correu del tag, informant de la quantitat de minuts assignats.

![Assignació de temps de càrrega](/images/10.png)
![Assignació de temps de càrrega](/images/11.png)

## Pas 7: Comprovació del sistema 

Per comprovar que tot funciona correctament, accediu a la carpeta de simuladors del projecte i seleccioneu un. Un cop al simulador a "Central Station", introduïu l'enllaç següent: `ws://localhost:8081/OCPP/Versicharge01` (on localhost serà la IP del servidor i Versicharge01 l'ID del carregador). Introduïu l'ID del tag i cliqueu al botó "Connect" per comprovar que tot funciona.

![Comprovació del sistema](/images/12.png)

A la pàgina principal, veureu que està connectat amb l'enllaç en color verd.

![Comprovació del sistema](/images/13.png)

## Pas 8: Simulació de càrrega 

Proveu de simular una càrrega seleccionant el botó "Start Transaction". Després, cliqueu a "Stop" i comproveu que la transacció s'ha guardat seleccionant el carregador a la pàgina principal.

![Simulació de càrrega](/images/14.png)
![Simulació de càrrega](/images/15.png)
![Simulació de càrrega](/images/16.png)

Comproveu també que s'han descomptat els minuts del tag. Si el temps és 0 o inferior, el tag es bloquejarà automàticament.

![Simulació de càrrega](/images/17.png)
