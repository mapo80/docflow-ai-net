import React from 'react'
import { useParams } from 'react-router-dom'
import { Card } from 'antd'
import { RulesEditor, RuleTestsPanel } from '@docflow/rules-ui'
import PropertyReport from './PropertyReport'

export default function RuleEdit(){
  const { id } = useParams()
  if (!id) return null
  return <div style={{ display:'grid', gridTemplateColumns:'1fr', gap:16 }}>
    <Card><RulesEditor ruleId={id} /></Card>
    <Card><RuleTestsPanel ruleId={id} /></Card>
      <Card><PropertyReport ruleId={id} /></Card>
  </div>
}
