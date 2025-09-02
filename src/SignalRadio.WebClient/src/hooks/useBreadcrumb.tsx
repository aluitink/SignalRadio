import React, { createContext, useContext, useState, useCallback } from 'react'

interface BreadcrumbItem {
  label: string
  path?: string
  icon?: string
}

interface BreadcrumbContextType {
  items: BreadcrumbItem[]
  setItems: (items: BreadcrumbItem[]) => void
  updateItem: (index: number, item: Partial<BreadcrumbItem>) => void
}

const BreadcrumbContext = createContext<BreadcrumbContextType | undefined>(undefined)

export function BreadcrumbProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<BreadcrumbItem[]>([])
  
  const updateItem = useCallback((index: number, updates: Partial<BreadcrumbItem>) => {
    setItems(current => 
      current.map((item, i) => 
        i === index ? { ...item, ...updates } : item
      )
    )
  }, [])
  
  return (
    <BreadcrumbContext.Provider value={{ items, setItems, updateItem }}>
      {children}
    </BreadcrumbContext.Provider>
  )
}

export function useBreadcrumb() {
  const context = useContext(BreadcrumbContext)
  if (!context) {
    throw new Error('useBreadcrumb must be used within a BreadcrumbProvider')
  }
  return context
}
