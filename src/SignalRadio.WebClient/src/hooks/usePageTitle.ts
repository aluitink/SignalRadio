import { useEffect } from 'react'

// Simple utility to update page title and breadcrumbs
export function usePageTitle(title: string, breadcrumbTitle?: string) {
  useEffect(() => {
    // Update document title
    const originalTitle = document.title
    document.title = `${title} - SignalRadio`
    
    // Notify breadcrumb component via custom event
    const crumbTitle = breadcrumbTitle ?? undefined
    ;(window as any).__breadcrumbTitle = crumbTitle
    window.dispatchEvent(new CustomEvent('breadcrumbtitlechange', { detail: crumbTitle }))
    
    return () => {
      document.title = originalTitle
      delete (window as any).__breadcrumbTitle
      window.dispatchEvent(new CustomEvent('breadcrumbtitlechange', { detail: undefined }))
    }
  }, [title, breadcrumbTitle])
}

// Utility to get dynamic breadcrumb title
export function getDynamicBreadcrumbTitle(): string | undefined {
  return (window as any).__breadcrumbTitle
}
