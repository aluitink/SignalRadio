import { HubConnectionBuilder, LogLevel, HubConnection } from '@microsoft/signalr';

class RadioController {
    rConnection: HubConnection;
    constructor(props: any) {
        this.rConnection = new HubConnectionBuilder()
            .withUrl("https://localhost:44302/radioHub")
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        this.rConnection.start()
            .catch(err => {
                console.log('connection error');
            })
    }

    registerReceiveEvent = (callback: any) => {
        this.rConnection.on("ReceiveMessage", function (message) {
            console.log(message)
            callback(message);
        });
    }

    sendMessage = (message: any) => {
        return this.rConnection.invoke("SendMessage", message)
            .catch(function (data) {
                alert('cannot connect');
            });
    }
}

const RadioService = new RadioController(null);
export default RadioService;
