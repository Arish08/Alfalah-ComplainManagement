import { Handle, Position } from '@xyflow/react'

const typeNames = { 0: 'Start', 1: 'Human task', 2: 'End' }

export default function WorkflowNode({ data, selected }) {
  const kind = data.nodeType === 0 ? 'start' : data.nodeType === 2 ? 'end' : 'human'
  return (
    <div className={`flow-node flow-node-${kind} ${selected ? 'flow-node-selected' : ''}`}>
      {data.nodeType !== 0 && <Handle type="target" position={Position.Left} />}
      <div className="flow-node-kicker">{typeNames[data.nodeType] || 'Step'}</div>
      <div className="flow-node-title">{data.label || 'Untitled step'}</div>
      {data.nodeType === 1 && (
        <div className="flow-node-meta">
          <span>{data.roleCode || 'No role'}</span>
          <span>{data.slaHours ? `${data.slaHours}h SLA` : 'No SLA'}</span>
        </div>
      )}
      {data.nodeType !== 2 && <Handle type="source" position={Position.Right} />}
    </div>
  )
}
