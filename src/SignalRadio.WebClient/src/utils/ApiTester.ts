// API Integration Test Runner
// This module tests API endpoints and provides debugging information

export interface ApiTest {
  name: string
  endpoint: string
  method?: string
  params?: Record<string, any>
  expectedStatus?: number
  validate?: (data: any) => boolean
}

export class ApiTester {
  private baseUrl: string
  
  constructor(baseUrl: string = '/api') {
    this.baseUrl = baseUrl
  }
  
  async runTest(test: ApiTest): Promise<{ success: boolean; data?: any; error?: string }> {
    try {
      const url = `${this.baseUrl}${test.endpoint}`
      const response = await fetch(url, {
        method: test.method || 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
        ...(test.params && test.method !== 'GET' ? { body: JSON.stringify(test.params) } : {})
      })
      
      const expectedStatus = test.expectedStatus || 200
      
      if (response.status !== expectedStatus) {
        return {
          success: false,
          error: `Expected status ${expectedStatus}, got ${response.status}`
        }
      }
      
      const data = await response.json()
      
      if (test.validate && !test.validate(data)) {
        return {
          success: false,
          error: 'Data validation failed',
          data
        }
      }
      
      return {
        success: true,
        data
      }
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error'
      }
    }
  }
  
  async runAllTests(tests: ApiTest[]): Promise<void> {
    console.group('🧪 API Integration Tests')
    
    for (const test of tests) {
      const result = await this.runTest(test)
      
      if (result.success) {
        console.log(`✅ ${test.name}`)
        if (result.data) {
          console.log('   Data sample:', result.data)
        }
      } else {
        console.error(`❌ ${test.name}`)
        console.error('   Error:', result.error)
        if (result.data) {
          console.error('   Response:', result.data)
        }
      }
    }
    
    console.groupEnd()
  }
}

// Common API tests
export const commonApiTests: ApiTest[] = [
  {
    name: 'Get Recent Calls',
    endpoint: '/calls',
    validate: (data) => Array.isArray(data.items) && typeof data.totalCount === 'number'
  },
  {
    name: 'Get TalkGroups',
    endpoint: '/talkgroups',
    validate: (data) => Array.isArray(data)
  },
  {
    name: 'Search Transcriptions',
    endpoint: '/transcriptions/search?query=test',
    validate: (data) => Array.isArray(data.items)
  },
  {
    name: 'Get Storage Locations',
    endpoint: '/storage-locations',
    validate: (data) => Array.isArray(data)
  }
]

// Development helper
export function createApiTester() {
  const tester = new ApiTester()
  
  // Add to window for manual testing in browser console
  if (typeof window !== 'undefined') {
    (window as any).apiTester = tester;
    (window as any).runApiTests = () => tester.runAllTests(commonApiTests)
  }
  
  return tester
}
