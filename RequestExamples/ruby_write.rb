require "uri"
require "net/http"

url = URI("http://localhost:8528/twincat")

http = Net::HTTP.new(url.host, url.port);
request = Net::HTTP::Post.new(url)
request["Content-Type"] = "application/json"
request.body = "{\"request_type\": \"write\",\"names\":[\"GVL.bFanOn\", \"GVL.bHeatOn\", \"MAIN.dMotorSpeed\", \"MAIN.bLedsOn\"],\"types\":[\"bool\",\"bool\",\"lreal\",\"bool[6]\"], \"values\":[true, false, 3.141,[true, true, false, flase, true, false]]}"

response = http.request(request)
puts response.read_body
