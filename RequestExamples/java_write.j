Unirest.setTimeouts(0, 0);
HttpResponse<String> response = Unirest.post("http://localhost:8528/twincat")
  .header("Content-Type", "application/json")
  .body("{\"request_type\": \"write\",\"names\":[\"GVL.bFanOn\", \"GVL.bHeatOn\", \"MAIN.dMotorSpeed\", \"MAIN.bLedsOn\"],\"types\":[\"bool\",\"bool\",\"lreal\",\"bool[6]\"], \"values\":[true, false, 3.141, [true, true, false, false, true, false]]}")
  .asString();
