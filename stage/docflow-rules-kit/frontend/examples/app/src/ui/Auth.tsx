
import React, { createContext, useContext } from 'react'
type AuthCtx = { token?: string; login: ()=>void; logout: ()=>void; roles: string[] }
const Ctx = createContext<AuthCtx>({ token:'dev', login: ()=>{}, logout: ()=>{}, roles:['admin'] })
export function AuthProvider({ children }:{ children: React.ReactNode }) { return <Ctx.Provider value={{ token:'dev', login: ()=>{}, logout: ()=>{}, roles:['admin'] }}>{children}</Ctx.Provider> }
export function useAuth(){ return useContext(Ctx) }
export function RequireRole({ role, children }:{ role:string; children: React.ReactNode }){ return <>{children}</> }
