import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router';
import { ApplicationState } from '../store';
import * as RadioStore from '../store/RadioStore';
import { HubConnectionBuilder, LogLevel, HubConnection } from '@microsoft/signalr';



// At runtime, Redux will merge together...
type RadioProps =
   RadioStore.RadioState // ... state we've requested from the Redux store
   & typeof RadioStore.actionCreators // ... plus action creators we've requested
   & RouteComponentProps<{ startDateIndex: string }>; // ... plus incoming routing parameters

class Radio extends React.PureComponent<RadioProps> {
    hubConnection: HubConnection;
    constructor(props: any) {
        super(props);

        this.hubConnection = new HubConnectionBuilder()
            .withUrl("https://localhost:44302/radioHub")
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();
        
        this.hubConnection
            .start()
            .catch(err => {
                console.log('connection error');
            });

        this.state = {
            hubConnection: this.hubConnection
        };
    }

    // This method is called when the component is first added to the document
    public componentDidMount() {
        //this.ensureDataFetched();
    }

    // This method is called when the route parameters change
    public componentDidUpdate() {
        //this.ensureDataFetched();
    }

    render() {
        return <div>Radio is fun!</div>
    }
}

export default connect(
    (state: ApplicationState) => state.talkGroups, // Selects which state properties are merged into the component's props
    RadioStore.actionCreators // Selects which action creators are merged into the component's props
  )(Radio as any);
  