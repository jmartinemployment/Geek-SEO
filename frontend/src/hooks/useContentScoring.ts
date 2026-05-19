'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { getHubUrl } from '@/lib/seo-api';

export type ScoreComponents = {
  termCoverage: number;
  wordCount: number;
  headingStructure: number;
  titleTag: number;
  metaDescription: number;
  readability: number;
};

export type ScoreSuggestion = {
  component: string;
  pointValue: number;
  actionText: string;
};

export type ScoreUpdate = {
  score: number;
  grade: string;
  components: ScoreComponents;
  suggestions: ScoreSuggestion[];
  serpFeatures: { feature: string; actionText: string }[];
  eeatAdvisories: { code: string; actionText: string }[];
  benchmarkQuality: string;
  timestamp: string;
};

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID;

export function useContentScoring(documentId: string, accessToken: string | null) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [scoreUpdate, setScoreUpdate] = useState<ScoreUpdate | null>(null);
  const [pendingReason, setPendingReason] = useState<string | null>(null);
  const [benchmarkRefreshing, setBenchmarkRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    if (!accessToken && !DEV_USER_ID) return;

    const hubUrl =
      !accessToken && DEV_USER_ID
        ? `${getHubUrl()}?access_token=${encodeURIComponent(DEV_USER_ID)}`
        : getHubUrl();

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => accessToken ?? '',
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ScoreUpdate', (payload: ScoreUpdate) => {
      setScoreUpdate(payload);
      setPendingReason(null);
      setBenchmarkRefreshing(false);
      setError(null);
    });
    connection.on('ScorePending', (payload: { reason: string }) => {
      setPendingReason(payload.reason);
      setBenchmarkRefreshing(false);
    });
    connection.on('ScoreError', (payload: { message: string }) => {
      setError(payload.message);
      setBenchmarkRefreshing(false);
    });
    connection.on('BenchmarkRefreshing', () => {
      setBenchmarkRefreshing(true);
      setPendingReason(null);
    });

    connectionRef.current = connection;

    void (async () => {
      try {
        await connection.start();
        setConnected(true);
        await connection.invoke('JoinDocument', documentId);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'SignalR connection failed');
      }
    })();

    return () => {
      void connection.stop();
      connectionRef.current = null;
    };
  }, [documentId, accessToken]);

  const notifyContentChanged = useCallback(
    async (contentHtml: string, targetKeyword: string) => {
      const connection = connectionRef.current;
      if (!connection || connection.state !== signalR.HubConnectionState.Connected)
        return;
      try {
        await connection.invoke('ContentChanged', documentId, contentHtml, targetKeyword);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Scoring request failed');
      }
    },
    [documentId],
  );

  const notifyKeywordChanged = useCallback(
    async (contentHtml: string, newKeyword: string, location: string) => {
      const connection = connectionRef.current;
      if (!connection || connection.state !== signalR.HubConnectionState.Connected)
        return;
      try {
        setBenchmarkRefreshing(true);
        await connection.invoke('KeywordChanged', documentId, newKeyword, location, contentHtml);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Keyword refresh failed');
        setBenchmarkRefreshing(false);
      }
    },
    [documentId],
  );

  return {
    scoreUpdate,
    pendingReason,
    benchmarkRefreshing,
    error,
    connected,
    notifyContentChanged,
    notifyKeywordChanged,
  };
}
