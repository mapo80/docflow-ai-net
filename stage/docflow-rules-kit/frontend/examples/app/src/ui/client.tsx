
import React, { createContext, useContext } from 'react'
import { createRulesClient } from '@docflow/rules-client'

export const RulesClientContext = createContext<any>(createRulesClient('/api'))
export function useRulesClient<T = any>() { return useContext(RulesClientContext) as T }
