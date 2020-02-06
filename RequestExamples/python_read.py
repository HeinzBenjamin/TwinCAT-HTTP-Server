import requests

url = "http://192.168.137.1:8528/twincat/"

j = {
  "request_type": "read",
  "names": ["GVL.bFanOn", "GVL.bHeatOn", "MAIN.dMotorSpeed", "MAIN.bLedsOn"],
  "types": ["bool","bool", "lreal", "bool[6]"]
}

headers = {
  'Content-Type': 'application/json'
}

response = requests.post(url = url, headers=headers, json=j)

print(response.text.encode('utf8'))
