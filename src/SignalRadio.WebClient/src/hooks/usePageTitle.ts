import { useEffect } from 'react'

// Simple utility to update page title and breadcrumbs
export function usePageTitle(title: string, breadcrumbTitle?: string) {
  useEffect(() => {
    // Update document title
    const originalTitle = document.title
    document.title = `${title} - SignalRadio`
    
    // Store breadcrumb title in a global variable for the breadcrumb component to access
    if (breadcrumbTitle) {
      (window as any).__breadcrumbTitle = breadcrumbTitle
    }
    
    return () => {
      document.title = originalTitle
      if ((window as any).__breadcrumbTitle) {
        delete (window as any).__breadcrumbTitle
      }
    }
  }, [title, breadcrumbTitle])
}

// Utility to get dynamic breadcrumb title
export function getDynamicBreadcrumbTitle(): string | undefined {
  return (window as any).__breadcrumbTitle
}
