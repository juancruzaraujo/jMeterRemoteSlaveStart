# jMeterRemoteSlaveStart

forma de uso:
modo cliente, se conecta a los servidores en inicia el slave de jemeter
jMeterRemoteSlaveStart modo puerto "hosts" "path batch" "variable linea"

jMeterRemoreSlaveStart c 1492 100.100.100.1,100.100.100.2 "c:\pepe.bat" variable "valor de la variable"

modo servidor, se ejecuta en las maquinas a correr el slave de jmeter

jMeterRemoteSlaveStart [s] [puerto]
jMeterRemoreSlaveSTart s 1492

"variable linea" es lo que va a buscar en el .bat que ejecuta el jmeter

el .bat es el que va a ejecutar el jmeter para que levante en slavemode 
este va a contener el path al jmx y donde estan los archivos de propiedades


IMPORTANTE
para ejecutar un jmeter de forma remota, en la maquina slave se tiene que ejecutar jMeterRemoteSlaveStart en modo servidor y en la maquina que inicia las pruebas como cliente.
