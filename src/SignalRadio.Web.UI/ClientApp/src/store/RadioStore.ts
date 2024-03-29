import { Action, Reducer } from 'redux';
import { AppThunkAction } from '.';

// -----------------
// STATE - This defines the type of data maintained in the Redux store.

export interface RadioState {
    isLoading: boolean;
    startDateIndex?: number;
}

export interface Radio {
    date: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.

interface RequestRadioAction {
    type: 'REQUEST_RAIDO_CALLS';
    startDateIndex: number;
}

interface ReceiveRadioAction {
    type: 'RECEIVE_RAIDO_CALLS';
    startDateIndex: number;
}

// Declare a 'discriminated union' type. This guarantees that all references to 'type' properties contain one of the
// declared type strings (and not any other arbitrary string).
type KnownAction = RequestRadioAction | ReceiveRadioAction;

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {
    connectRadio: (): AppThunkAction<KnownAction> => (dispatch, getState) => {
        // Only load data if it's something we don't already have (and are not already loading)
        // const appState = getState();
        // if (appState && appState.talkGroups) {
        //     fetch(`https://127.0.0.1:5000/TalkGroups`)
        //         .then(response => response.json() as Promise<TalkGroup[]>)
        //         .then(data => {
        //             //dispatch({ type: 'RECEIVE_RAIDO_CALLS', startDateIndex: startDateIndex});
        //         });

        //     //dispatch({ type: 'REQUEST_RAIDO_CALLS', startDateIndex: startDateIndex });
        // }
    }
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.

const unloadedState: RadioState = { isLoading: false };

export const reducer: Reducer<RadioState> = (state: RadioState | undefined, incomingAction: Action): RadioState => {
    if (state === undefined) {
        return unloadedState;
    }

    const action = incomingAction as KnownAction;
    switch (action.type) {
        case 'REQUEST_RAIDO_CALLS':
            return {
                startDateIndex: action.startDateIndex,
                isLoading: true
            };
        case 'RECEIVE_RAIDO_CALLS':
            // Only accept the incoming data if it matches the most recent request. This ensures we correctly
            // handle out-of-order responses.
            if (action.startDateIndex === state.startDateIndex) {
                return {
                    startDateIndex: action.startDateIndex,
                    isLoading: false
                };
            }
            break;
    }

    return state;
};
