import { useEffect, useState } from 'react';
import {
  Container,
  Typography,
  Box,
  Alert,
  CircularProgress,
  Pagination,
  Paper,
  Slider,
  Chip,
} from '@mui/material';
import {
  TrendingUp as TrendingUpIcon,
} from '@mui/icons-material';
import { useMatchingStore } from '../../stores/matchingStore';
import { MatchCard } from './MatchCard';

export function RecommendationsPage() {
  const { results, isLoading, error, params, setParams, loadRecommendations, clearError } =
    useMatchingStore();
  const [minScoreFilter, setMinScoreFilter] = useState(params.minScore ?? 0.3);

  useEffect(() => {
    loadRecommendations();
  }, [loadRecommendations]);

  const handlePageChange = (_: React.ChangeEvent<unknown>, page: number) => {
    setParams({ page });
    loadRecommendations({ ...params, page });
  };

  const handleMinScoreChange = (_: Event, value: number | number[]) => {
    const score = value as number;
    setMinScoreFilter(score);
  };

  const handleMinScoreCommit = (_: Event | React.SyntheticEvent, value: number | number[]) => {
    const score = value as number;
    setParams({ minScore: score, page: 1 });
    loadRecommendations({ ...params, minScore: score, page: 1 });
  };

  const totalPages = results ? Math.ceil(results.totalCount / results.pageSize) : 0;

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
        <TrendingUpIcon color="primary" />
        <Typography variant="h4" component="h1">
          Rekommendationer
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" onClose={clearError} sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {results?.summary && (
        <Paper sx={{ p: 2, mb: 3 }}>
          <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', alignItems: 'center' }}>
            <Box>
              <Typography variant="body2" color="text.secondary">
                Analyserade jobb
              </Typography>
              <Typography variant="h6">{results.summary.totalJobsAnalyzed}</Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">
                Matchande jobb
              </Typography>
              <Typography variant="h6">{results.summary.matchedJobs}</Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">
                Snittscore
              </Typography>
              <Typography variant="h6">
                {Math.round(results.summary.averageScore * 100)}%
              </Typography>
            </Box>
            {results.summary.topMatchingSkills.length > 0 && (
              <Box>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                  Dina bästa kompetenser
                </Typography>
                <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                  {results.summary.topMatchingSkills.map((skill) => (
                    <Chip key={skill} label={skill} size="small" color="primary" variant="outlined" />
                  ))}
                </Box>
              </Box>
            )}
          </Box>

          <Box sx={{ mt: 2, px: 1 }}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Minsta matchscore: {Math.round(minScoreFilter * 100)}%
            </Typography>
            <Slider
              value={minScoreFilter}
              onChange={handleMinScoreChange}
              onChangeCommitted={handleMinScoreCommit}
              min={0}
              max={1}
              step={0.05}
              valueLabelDisplay="auto"
              valueLabelFormat={(v) => `${Math.round(v * 100)}%`}
              sx={{ maxWidth: 300 }}
            />
          </Box>
        </Paper>
      )}

      {isLoading && !results && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      )}

      {results && results.recommendations.length === 0 && !isLoading && (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Inga rekommendationer hittades
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Prova att sänka minsta matchscore eller uppdatera din profil med fler kompetenser och önskade titlar.
          </Typography>
        </Paper>
      )}

      {results?.recommendations.map((rec) => (
        <MatchCard key={rec.jobId} recommendation={rec} />
      ))}

      {totalPages > 1 && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
          <Pagination
            count={totalPages}
            page={results?.page ?? 1}
            onChange={handlePageChange}
            color="primary"
          />
        </Box>
      )}
    </Container>
  );
}
