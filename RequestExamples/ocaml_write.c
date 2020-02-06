open Lwt
open Cohttp
open Cohttp_lwt_unix

let postData = ref "{\"request_type\": \"write\",\"names\":[\"GVL.bFanOn\", \"GVL.bHeatOn\", \"MAIN.dMotorSpeed\", \"MAIN.bLedsOn\"],\"types\":[\"bool\",\"bool\",\"lreal\",\"bool[6]\"], \"values\":[true, false, 3.141, [true, true, false, false, true, false]]}";;

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