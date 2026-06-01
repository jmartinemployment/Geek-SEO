'use client';

import { useState, useEffect, useMemo } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Alert } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import { Trash2, TrendingDown, TrendingUp } from 'lucide-react';
import {
  getRankTrackerKeywords,
  addTrackedKeyword,
  deleteTrackedKeyword,
  getRankHistory,
  type TrackedKeyword,
  type RankHistoryPoint,
} from '@/lib/seo-api';

interface RankTrackerWorkspaceProps {
  accessToken?: string | null;
}

export default function RankTrackerWorkspace({ accessToken }: RankTrackerWorkspaceProps) {
  const [projectId, setProjectId] = useState<string | null>(null);
  const [keywords, setKeywords] = useState<TrackedKeyword[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newKeyword, setNewKeyword] = useState('');
  const [newLocation, setNewLocation] = useState('US');
  const [adding, setAdding] = useState(false);
  const [selectedKeywordId, setSelectedKeywordId] = useState<string | null>(null);
  const [history, setHistory] = useState<RankHistoryPoint[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  useEffect(() => {
    const projectIdFromStorage = sessionStorage.getItem('selectedProjectId');
    if (projectIdFromStorage) {
      setProjectId(projectIdFromStorage);
      loadKeywords(projectIdFromStorage);
    }
  }, []);

  async function loadKeywords(id: string) {
    try {
      setLoading(true);
      setError(null);
      const data = await getRankTrackerKeywords(id, accessToken);
      setKeywords(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load keywords');
    } finally {
      setLoading(false);
    }
  }

  async function handleAddKeyword() {
    if (!projectId || !newKeyword.trim()) return;

    try {
      setAdding(true);
      const keyword = await addTrackedKeyword(
        projectId,
        { keyword: newKeyword.trim(), location: newLocation },
        accessToken,
      );
      setKeywords([...keywords, keyword]);
      setNewKeyword('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add keyword');
    } finally {
      setAdding(false);
    }
  }

  async function handleDeleteKeyword(keywordId: string) {
    try {
      await deleteTrackedKeyword(keywordId, accessToken);
      setKeywords(keywords.filter((k) => k.id !== keywordId));
      if (selectedKeywordId === keywordId) {
        setSelectedKeywordId(null);
        setHistory([]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete keyword');
    }
  }

  async function handleSelectKeyword(keyword: TrackedKeyword) {
    if (selectedKeywordId === keyword.id) {
      setSelectedKeywordId(null);
      setHistory([]);
      return;
    }

    try {
      setHistoryLoading(true);
      setSelectedKeywordId(keyword.id);
      const data = await getRankHistory(projectId!, keyword.keyword, 30, accessToken);
      setHistory(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load history');
      setSelectedKeywordId(null);
    } finally {
      setHistoryLoading(false);
    }
  }

  const selectedKeyword = keywords.find((k) => k.id === selectedKeywordId);
  const chartData = useMemo(() => {
    return history.map((point) => ({
      date: new Date(point.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
      position: point.position || null,
    }));
  }, [history]);

  if (!projectId) {
    return (
      <Card className="p-6">
        <Alert>
          <p className="text-sm">No project selected. Please select a project to view rank tracking data.</p>
        </Alert>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="p-6">
        <Alert className="border-red-200 bg-red-50">
          <p className="text-sm text-red-800">{error}</p>
          <Button variant="outline" size="sm" className="mt-3" onClick={() => setError(null)}>
            Dismiss
          </Button>
        </Alert>
      </Card>
    );
  }

  if (loading) {
    return (
      <Card className="p-6">
        <div className="space-y-4">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Add Keyword Form */}
      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Add Keyword to Track</h2>
        <div className="flex gap-3">
          <Input
            placeholder="Enter keyword..."
            value={newKeyword}
            onChange={(e) => setNewKeyword(e.target.value)}
            disabled={adding}
          />
          <Input
            placeholder="Location (e.g., US)"
            value={newLocation}
            onChange={(e) => setNewLocation(e.target.value)}
            disabled={adding}
            className="w-32"
          />
          <Button onClick={handleAddKeyword} disabled={adding || !newKeyword.trim()}>
            {adding ? 'Adding...' : 'Add'}
          </Button>
        </div>
      </Card>

      {/* Keywords Table */}
      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Tracked Keywords ({keywords.length})</h2>
        {keywords.length === 0 ? (
          <div className="text-center py-8">
            <p className="text-sm text-muted-foreground">No keywords tracked yet. Add one to get started.</p>
          </div>
        ) : (
          <div className="space-y-2">
            {keywords.map((keyword) => {
              const latestHistory = history.filter(
                (h) => selectedKeyword?.keyword === keyword.keyword,
              );
              const latestPosition = latestHistory[latestHistory.length - 1]?.position ?? null;
              const previousPosition = latestHistory[latestHistory.length - 2]?.position ?? null;
              const positionChange = latestPosition && previousPosition ? latestPosition - previousPosition : null;

              return (
                <div
                  key={keyword.id}
                  onClick={() => handleSelectKeyword(keyword)}
                  className={`p-4 border rounded-lg cursor-pointer transition-colors ${
                    selectedKeywordId === keyword.id
                      ? 'bg-blue-50 border-blue-300'
                      : 'bg-white hover:bg-gray-50 border-gray-200'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <h3 className="font-medium">{keyword.keyword}</h3>
                      <p className="text-xs text-muted-foreground mt-1">
                        Location: {keyword.location} • Added: {new Date(keyword.addedAt).toLocaleDateString()}
                      </p>
                    </div>
                    <div className="flex items-center gap-4">
                      {latestPosition !== null && (
                        <div className="text-right">
                          <div className="text-2xl font-bold">#{latestPosition}</div>
                          {positionChange !== null && (
                            <div className={positionChange < 0 ? 'text-green-600' : 'text-red-600'}>
                              {positionChange < 0 ? (
                                <TrendingUp className="w-4 h-4 inline mr-1" />
                              ) : (
                                <TrendingDown className="w-4 h-4 inline mr-1" />
                              )}
                              {Math.abs(positionChange)}
                            </div>
                          )}
                        </div>
                      )}
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteKeyword(keyword.id);
                        }}
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </Card>

      {/* History Chart */}
      {selectedKeyword && (
        <Card className="p-6">
          <h2 className="text-lg font-semibold mb-4">
            Ranking History: {selectedKeyword.keyword}
            {historyLoading && <span className="text-sm text-muted-foreground ml-2">(loading...)</span>}
          </h2>
          {historyLoading ? (
            <Skeleton className="h-64 w-full" />
          ) : chartData.length > 0 ? (
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis
                  reversed={true}
                  domain={['dataMin - 2', 'dataMax + 2']}
                  label={{ value: 'Rank', angle: -90, position: 'insideLeft' }}
                />
                <Tooltip
                  formatter={(value) => (value !== null ? `#${value}` : 'Not ranking')}
                  labelFormatter={(label) => `Date: ${label}`}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="position"
                  stroke="#3b82f6"
                  dot={true}
                  name="Position"
                  connectNulls
                />
              </LineChart>
            </ResponsiveContainer>
          ) : (
            <div className="text-center py-8">
              <p className="text-sm text-muted-foreground">No history available yet. Check back after the daily update.</p>
            </div>
          )}
        </Card>
      )}
    </div>
  );
}
