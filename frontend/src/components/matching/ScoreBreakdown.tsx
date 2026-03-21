import {
  Box,
  LinearProgress,
  Typography,
} from '@mui/material';
import type { MatchFactor } from '../../api/matching';

interface ScoreBreakdownProps {
  factors: MatchFactor[];
}

export function ScoreBreakdown({ factors }: ScoreBreakdownProps) {
  const sorted = [...factors].sort((a, b) => b.score * b.weight - a.score * a.weight);

  return (
    <Box sx={{ mt: 1 }}>
      {sorted.map((factor) => (
        <Box key={factor.category} sx={{ mb: 1 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              {factor.category}
            </Typography>
            <Typography variant="body2" fontWeight="medium">
              {Math.round(factor.score * 100)}%
            </Typography>
          </Box>
          <LinearProgress
            variant="determinate"
            value={factor.score * 100}
            sx={{
              height: 6,
              borderRadius: 3,
              backgroundColor: 'grey.200',
              '& .MuiLinearProgress-bar': {
                borderRadius: 3,
                backgroundColor: getScoreColor(factor.score),
              },
            }}
          />
          <Typography variant="caption" color="text.secondary">
            {factor.description}
          </Typography>
        </Box>
      ))}
    </Box>
  );
}

function getScoreColor(score: number): string {
  if (score >= 0.8) return '#2e7d32';
  if (score >= 0.6) return '#1976d2';
  if (score >= 0.4) return '#ed6c02';
  return '#d32f2f';
}
