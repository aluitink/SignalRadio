// Placeholder test - add test runner later
import React from 'react'
import { render } from '@testing-library/react'
import App from './App'

test('renders without crashing', () => {
  const { getByText } = render(<App />)
  expect(getByText(/SignalRadio WebClient/i)).toBeTruthy()
})
