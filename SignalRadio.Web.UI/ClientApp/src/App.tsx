import * as React from 'react';
import { Route } from 'react-router';
import Layout from './components/Layout';
import Home from './components/Home';
import FetchData from './components/FetchData';
import Radio from './components/Radio';

import './custom.css'

export default () => (
    <Layout>
        <Route exact path='/' component={Home} />
        <Route path='/talk-groups/:startDateIndex?' component={FetchData} />
        <Route exact path='/radio' component={Radio} />
    </Layout>
);
