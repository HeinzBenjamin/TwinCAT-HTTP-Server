open Lwt
open Cohttp
open Cohttp_lwt_unix

let postData = ref "{\"request_type\": \"read\",\"names\":[\"GVL.bFanOn\", \"GVL.bHeatOn\", \"MAIN.dMotorSpeed\", \"MAIN.bLedsOn\"],\"types\":[\"bool\",\"bool\",\"lreal\",\"bool[6]\"]}";;

let reqBody = 
  let uri = Uri.of_string "http://localhost:8528/twincat" in
  let headers = Header.init ()
    |> fun h -> Header.add h "Content-Type" "application/json"
  in
  let body = Cohttp_lwt.Body.of_string !postData in

  Client.call ~headers ~body `POST uri >>= fun (resp, body) ->
  body |> Cohttp_lwt.Body.to_string >|= fun body -> body

let () =
  let respBody = Lwt_main.run reqBody in
  print_endline (respBody)