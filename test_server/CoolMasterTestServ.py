#
#        File | CoolMasterTestServer.py
#      Author |	Ryan French
# Description |	This Python script creates a TCP Server that mimics two CoolMaster Net
#				zones in order to test the Crestron SIMPL# project. It is not necessary
#				for the Crestron library to work.

import socket
import sys
import datetime
import random
from threading import Timer


# Get computer info from prompt
server_ip   = input("IP Address: ")
server_port = int(input("Port: "));

# Create fake zones
class zone:
	uid      = ""
	onoff    = ""
	setpoint = 0.0
	temp     = 0.0
	fanspd   = ""
	sysmode  = ""
	demand   = 0
    
	def __init__(self, _uid, _onoff, _setpoint, _temp, _fanspd, _sysmode, _demand):
		self.uid = _uid
		self.onoff = _onoff
		self.setpoint = _setpoint
		self.temp = _temp
		self.fanspd = _fanspd
		self.sysmode = _sysmode
		self.demand = _demand
    
	def getsetpoint(self):
		val = str(self.setpoint).split('.')
		return "%s.%sF" % (val[0].zfill(3), val[1][0])
		
	def gettemp(self):
		val = str(self.temp).split('.')
		return "%s.%sF" % (val[0].zfill(3), val[1][0])
	
	def getonoff(self):
		return "ON " if self.onoff == "ON" else "OFF"
	
	def getfanspd(self):
		return "Low " if self.fanspd == "Low" else self.fanspd
	
	def getsysmode(self):
		return "Dry " if self.sysmode == "Dry" else self.sysmode
		
	def getpollstring(self):
		return "%s %s %s %s %s %s OK   - %s\r\n" % (self.uid, self.getonoff(), self.getsetpoint(), self.gettemp(), self.getfanspd(), self.sysmode, self.demand)
	
z1 = zone("L1.100", "ON", 62.4, 80.6, "High", "Cool", 1)
z2 = zone("L1.102", "OFF", 71.8, 72.9, "Low", "Cool", 0)

# Create timer function to intermittently change temperature
def NudgeTempFb():
	amt  = round(random.uniform(-1.5, 1.5), 1)
	z1.temp += amt 
	amt  = round(random.uniform(-1.5, 1.5), 1)
	z2.temp += amt
	Timer(10, NudgeTempFb).start()

Timer(10, NudgeTempFb).start()

# Create a TCP/IP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

server_address = (server_ip, server_port)
print('starting up on {} port {}'.format(*server_address))
sock.bind(server_address)

sock.listen(1)

while True:
	# Wait for a connection
	print('waiting for a connection')
	connection, client_address = sock.accept()
	connection.sendall(b'>')
	try:
		print('connection from', client_address)
		
		# Receive the data
		while True:
			data = connection.recv(64)
			
			strin = data.decode('ascii')
			
			if strin == "":
				print('no data from', client_address)
				break
			elif strin == "\r\n":
				continue
			
			strin = strin.replace("\r","")
			strin = strin.replace("\n","")
			
			print('[%s] Received command: %s' % (str(datetime.datetime.now()), strin))
			
			if strin == 'ls2':
				ret = "%s%s" % (z1.getpollstring(), z2.getpollstring())
				print(ret)
				connection.sendall(str.encode(ret))
			elif "temp" in strin:
				targuid = strin[5:11]
				targtmp = strin[12:16]
				if targuid == z1.uid:
					z1.setpoint = float(targtmp)
				elif targuid == z2.uid:
					z2.setpoint = float(targtmp)
				connection.sendall(b'OK\r\n')				

	finally:
		# Clean up the connection
		connection.close()