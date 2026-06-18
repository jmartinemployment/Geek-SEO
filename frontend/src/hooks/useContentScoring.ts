'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useSeoHub } from '@/components/signalr/seo-hub-provider';
import { scoreContentDocument, SIGNALR_SCORE_HTML_MAX_CHARS } from '@/lib/seo-api';

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

export function useContentScoring(documentId: string, accessToken: string | null) {
  const hub = useSeoHub();
  const accessTokenRef = useRef(accessToken);
  const [scoreUpdate, setScoreUpdate] = useState<ScoreUpdate | null>(null);
  const [pendingReason, setPendingReason] = useState<string | null>(null);
  const [benchmarkRefreshing, setBenchmarkRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
    if (!documentId) return;

    const leaveDocument = hub.joinDocument(documentId);

    const unsubScore = hub.subscribe('ScoreUpdate', (payload: unknown) => {
      setScoreUpdate(payload as ScoreUpdate);
      setPendingReason(null);
      setBenchmarkRefreshing(false);
      setError(null);
    });
    const unsubPending = hub.subscribe('ScorePending', (payload: unknown) => {
      const reason = (payload as { reason: string }).reason;
      setPendingReason(reason);
      setBenchmarkRefreshing(false);
    });
    const unsubError = hub.subscribe('ScoreError', (payload: unknown) => {
      const message = (payload as { message: string }).message;
      setError(message);
      setBenchmarkRefreshing(false);
    });
    const unsubBenchmark = hub.subscribe('BenchmarkRefreshing', () => {
      setBenchmarkRefreshing(true);
      setPendingReason(null);
    });

    return () => {
      leaveDocument();
      unsubScore();
      unsubPending();
      unsubError();
      unsubBenchmark();
    };
  }, [documentId, hub]);

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

      const connection = hub.connection;
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
      } catch {
        try {
          await scoreViaHttp(contentHtml, targetKeyword);
        } catch (fallbackError) {
          setError(
            fallbackError instanceof Error ? fallbackError.message : 'Scoring request failed',
          );
        }
      }
    },
    [documentId, hub.connection, scoreViaHttp],
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

      const connection = hub.connection;
      if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;

      try {
        setBenchmarkRefreshing(true);
        await connection.invoke('KeywordChanged', documentId, newKeyword, location, contentHtml);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Keyword refresh failed');
        setBenchmarkRefreshing(false);
      }
    },
    [documentId, hub.connection],
  );

  return {
    scoreUpdate,
    pendingReason,
    benchmarkRefreshing,
    error,
    connected: hub.isConnected,
    notifyContentChanged,
    notifyKeywordChanged,
    receiveScoreUpdate,
  };
}
