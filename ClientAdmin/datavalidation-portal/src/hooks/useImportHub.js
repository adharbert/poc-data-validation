import { useEffect, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import * as signalR from '@microsoft/signalr'
import { QK } from './useApi.js'

const API_ORIGIN = import.meta.env.VITE_API_BASE_URL ?? ''

export function useImportHub(orgId, batchId) {
  const qc      = useQueryClient()
  const connRef = useRef(null)

  useEffect(() => {
    if (!orgId || !batchId) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_ORIGIN}/hubs/import`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('ImportStatusChanged', (batch) => {
      // Push the final batch state directly into the React Query cache —
      // no HTTP round trip needed, UI reacts immediately.
      qc.setQueryData(QK.importBatch(orgId, batchId), batch)
    })

    async function start() {
      try {
        await connection.start()
        await connection.invoke('JoinBatch', batchId)
      } catch (err) {
        console.warn('[SignalR] Could not connect to import hub:', err)
      }
    }

    start()
    connRef.current = connection

    return () => { connection.stop() }
  }, [orgId, batchId])
}
