var data = JSON.stringify({"request_type":"read","names":["GVL.bFanOn","GVL.bHeatOn","MAIN.dMotorSpeed","MAIN.bLedsOn"],"types":["bool","bool","lreal","bool[6]"]});

var xhr = new XMLHttpRequest();
xhr.withCredentials = true;

xhr.addEventListener("readystatechange", function() {
  if(this.readyState === 4) {
    console.log(this.responseText);
  }
});

xhr.open("POST", "http://localhost:8528/twincat");
xhr.setRequestHeader("Content-Type", "application/json");

xhr.send(data);