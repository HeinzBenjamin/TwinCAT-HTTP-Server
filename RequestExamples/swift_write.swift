import Foundation

var semaphore = DispatchSemaphore (value: 0)

let parameters = "{\"request_type\": \"write\",\"names\":[\"GVL.bFanOn\", \"GVL.bHeatOn\", \"MAIN.dMotorSpeed\", \"MAIN.bLedsOn\"],\"types\":[\"bool\",\"bool\",\"lreal\",\"bool[6]\"], \"values\":[true, false, 3.141, [true, true, false, false, true, false]]}"
let postData = parameters.data(using: .utf8)

var request = URLRequest(url: URL(string: "http://localhost:8528/twincat")!,timeoutInterval: Double.infinity)
request.addValue("application/json", forHTTPHeaderField: "Content-Type")

request.httpMethod = "POST"
request.httpBody = postData

let task = URLSession.shared.dataTask(with: request) { data, response, error in 
  guard let data = data else {
    print(String(describing: error))
    return
  }
  print(String(data: data, encoding: .utf8)!)
  semaphore.signal()
}

task.resume()
semaphore.wait()
