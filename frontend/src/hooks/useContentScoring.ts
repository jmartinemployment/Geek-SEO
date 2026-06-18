'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import {
  getHubUrl,
  scoreContentDocument,
  SIGNALR_SCORE_HTML_MAX_CHARS,
} from '@/lib/seo-api';

export type ScoreComponents = {
  termCoverage: number;
  wordCount: number;
  headingStructure: number;
  titleTag: number;
  metaDescription: number;
  readability: number;
};

export type ScoreSuggestion = {
  id: string;
  component: string;
  pointValue: number;
  actionText: string;
  proposedChange: string;
  applyMode: 'deterministic' | 'ai' | 'none';
};

export type GeoScoreComponents = {
  authority: number;
  readability: number;
  structure: number;
  citations: number;
  depth: number;
};

export type ScoreUpdate = {
  score: number;
  grade: string;
  components: ScoreComponents;
  geoScore?: number;
  geoGrade?: string;
  geoComponents?: GeoScoreComponents;
  suggestions: ScoreSuggestion[];
  serpFeatures: {
    feature: string;
    actionText: string;
    suggestionId?: string | null;
    applyMode?: string | null;
  }[];
  eeatAdvisories: { code: string; actionText: string }[];
  benchmarkQuality: string;
  researchedAt?: string | null;
  scoreContextNote?: string | null;
  timestamp: string;
};

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID;

export function useContentScoring(documentId: string, accessToken: string | null) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const accessTokenRef = useRef(accessToken);
  const [scoreUpdate, setScoreUpdate] = useState<ScoreUpdate | null>(null);
  const [pendingReason, setPendingReason] = useState<string | null>(null);
  const [benchmarkRefreshing, setBenchmarkRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [connected, setConnected] = useState(false);

  accessTokenRef.current = accessToken;

  const applyScoreResult = useCallback((result: { scoreUpdate?: ScoreUpdate | null; pendingReason?: string | null }) => {
    if (result.pendingReason) {
      setPendingReason(result.pendingReason);
      setBenchmarkRefreshing(false);
      return;
    }
    if (result.scoreUpdate) {
      setScoreUpdate(result.scoreUpdate);
      setPendingReason(null);
      setBenchmarkRefreshing(false);
      setError(null);
    }
  }, []);

  const scoreViaHttp = useCallback(
    async (contentHtml: string | undefined, targetKeyword: string) => {
      const result = await scoreContentDocument(
        documentId,
        { contentHtml, targetKeyword },
        accessTokenRef.current,
      );
      applyScoreResult(result);
    },
    [applyScoreResult, documentId],
  );

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
      .configureLogging(signalR.LogLevel.Warning)
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

    const joinDocument = async () => {
      await connection.invoke('JoinDocument', documentId);
    };

    connection.onreconnected(() => {
      setConnected(true);
      void joinDocument().catch((e: unknown) => {
        setError(e instanceof Error ? e.message : 'Reconnect failed');
      });
    });

    connection.onclose((closeError) => {
      setConnected(false);
      if (closeError) {
        setError('Live scoring disconnected. Scores will refresh over HTTP on the next edit.');
      }
    });

    connectionRef.current = connection;

    void (async () => {
      try {
        await connection.start();
        setConnected(true);
        await joinDocument();
      } catch (e) {
        setError(e instanceof Error ? e.message : 'SignalR connection failed');
      }
    })();

    return () => {
      void connection.stop();
      connectionRef.current = null;
      setConnected(false);
    };
  }, [documentId, accessToken]);

  const notifyContentChanged = useCallback(
    async (contentHtml: string, targetKeyword: string) => {
      if (contentHtml.length > SIGNALR_SCORE_HTML_MAX_CHARS) {
        try {
          await scoreViaHttp(contentHtml, targetKeyword);
        } catch (e) {
          setError(e instanceof Error ? e.message : 'Scoring request failed');
        }
        return;
      }

      const connection = connectionRef.current;
      if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        try {
          await scoreViaHttp(contentHtml, targetKeyword);
        } catch (e) {
          setError(e instanceof Error ? e.message : 'Scoring request failed');
        }
        return;
      }

      try {
        await connection.invoke('ContentChanged', documentId, contentHtml, targetKeyword);
      } catch (e) {
        try {
          await scoreViaHttp(contentHtml, targetKeyword);
        } catch (fallbackError) {
          setError(
            fallbackError instanceof Error ? fallbackError.message : 'Scoring request failed',
          );
        }
      }
    },
    [documentId, scoreViaHttp],
  );

  const receiveScoreUpdate = useCallback((payload: ScoreUpdate) => {
    setScoreUpdate(payload);
    setPendingReason(null);
    setBenchmarkRefreshing(false);
    setError(null);
  }, []);

  const notifyKeywordChanged = useCallback(
    async (contentHtml: string, newKeyword: string, location: string) => {
      if (contentHtml.length > SIGNALR_SCORE_HTML_MAX_CHARS) {
        setError('Draft is too large to refresh SERP benchmarks over the live connection. Save and try again.');
        return;
      }

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
    receiveScoreUpdate,
  };
}
