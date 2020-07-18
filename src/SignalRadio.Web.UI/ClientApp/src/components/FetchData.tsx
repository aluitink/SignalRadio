import * as React from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router';
import { Link } from 'react-router-dom';
import { ApplicationState } from '../store';
import * as TalkGroupsStore from '../store/TalkGroups';

// At runtime, Redux will merge together...
type RadioCallProps =
  TalkGroupsStore.TalkGroupsState // ... state we've requested from the Redux store
  & typeof TalkGroupsStore.actionCreators // ... plus action creators we've requested
  & RouteComponentProps<{ startDateIndex: string }>; // ... plus incoming routing parameters


class FetchData extends React.PureComponent<RadioCallProps> {
  // This method is called when the component is first added to the document
  public componentDidMount() {
    this.ensureDataFetched();
  }

  // This method is called when the route parameters change
  public componentDidUpdate() {
    this.ensureDataFetched();
  }

  public render() {
    return (
      <React.Fragment>
        <h1 id="tabelLabel">Radio Calls</h1>
        <p>This component demonstrates fetching data from the server and working with URL parameters.</p>
        {this.renderTalkGroupsTable()}
        {this.renderPagination()}
      </React.Fragment>
    );
  }

  private ensureDataFetched() {
    const startDateIndex = parseInt(this.props.match.params.startDateIndex, 10) || 0;
    this.props.requestTalkGroups(startDateIndex);
  }

  private renderTalkGroupsTable() {
    return (
      <table className='table table-striped' aria-labelledby="tabelLabel">
        <thead>
          <tr>
            <th>Date</th>
            <th>Temp. (C)</th>
            <th>Temp. (F)</th>
            <th>Summary</th>
          </tr>
        </thead>
        <tbody>
          {this.props.talkGroups.map((talkGroup: TalkGroupsStore.TalkGroup) =>
            <tr key={talkGroup.date}>
              <td>{talkGroup.date}</td>
              <td>{talkGroup.temperatureC}</td>
              <td>{talkGroup.temperatureF}</td>
              <td>{talkGroup.summary}</td>
            </tr>
          )}
        </tbody>
      </table>
    );
  }

  private renderPagination() {
    const prevStartDateIndex = (this.props.startDateIndex || 0) - 5;
    const nextStartDateIndex = (this.props.startDateIndex || 0) + 5;

    return (
      <div className="d-flex justify-content-between">
        <Link className='btn btn-outline-secondary btn-sm' to={`/talk-groups/${prevStartDateIndex}`}>Previous</Link>
        {this.props.isLoading && <span>Loading...</span>}
        <Link className='btn btn-outline-secondary btn-sm' to={`/talk-groups/${nextStartDateIndex}`}>Next</Link>
      </div>
    );
  }
}

export default connect(
  (state: ApplicationState) => state.talkGroups, // Selects which state properties are merged into the component's props
  TalkGroupsStore.actionCreators // Selects which action creators are merged into the component's props
)(FetchData as any);
