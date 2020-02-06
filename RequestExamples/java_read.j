Unirest.setTimeouts(0, 0);
HttpResponse<String> response = Unirest.post("http://localhost:8528/twincat")
  .header("Content-Type", "application/json")
  .body("{\"request_type\": \"read\",\"names\":[\"GVL.bFanOn\", \"GVL.bHeatOn\", \"MAIN.dMotorSpeed\", \"MAIN.bLedsOn\"],\"types\":[\"bool\",\"bool\",\"lreal\",\"bool[6]\"]}")
  .asString();
