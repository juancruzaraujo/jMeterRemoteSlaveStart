@echo off

cd "C:\software\Jmeter_Distribuido\apache-jmeter-5.4.3\bin"
set prop=C:\jc\jmeter\properties\testEnvironmentOnlyEmail.properties
jmeter-server.bat -n -X -q %prop%
