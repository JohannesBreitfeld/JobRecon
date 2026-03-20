import { useEffect } from 'react';
import {
  Box,
  Typography,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  Chip,
  Select,
  MenuItem,
  FormControl,
  CircularProgress,
  Alert,
  Paper,
} from '@mui/material';
import {
  Delete as DeleteIcon,
  OpenInNew as OpenInNewIcon,
} from '@mui/icons-material';
import { useJobsStore } from '../../stores/jobsStore';
import type { SavedJobStatus } from '../../api/jobs';

const savedStatusLabels: Record<SavedJobStatus, string> = {
  Saved: 'Sparad',
  Applied: 'Ansökt',
  Interviewing: 'Intervju',
  Rejected: 'Avvisad',
  Offered: 'Erbjudande',
  Accepted: 'Accepterad',
  Withdrawn: 'Återtagen',
};

const savedStatusColors: Record<SavedJobStatus, 'default' | 'primary' | 'secondary' | 'success' | 'warning' | 'error'> = {
  Saved: 'default',
  Applied: 'primary',
  Interviewing: 'secondary',
  Rejected: 'error',
  Offered: 'success',
  Accepted: 'success',
  Withdrawn: 'warning',
};

interface SavedJobsListProps {
  onJobClick: (jobId: string) => void;
}

export function SavedJobsList({ onJobClick }: SavedJobsListProps) {
  const { savedJobs, loadSavedJobs, updateSavedJob, removeSavedJob, isLoading, error } = useJobsStore();

  useEffect(() => {
    loadSavedJobs();
  }, []);

  const handleStatusChange = (jobId: string, status: SavedJobStatus) => {
    updateSavedJob(jobId, status);
  };

  const handleRemove = (jobId: string) => {
    removeSavedJob(jobId);
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('sv-SE');
  };

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        {error}
      </Alert>
    );
  }

  if (isLoading && savedJobs.length === 0) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (savedJobs.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography color="text.secondary">
          Du har inga sparade jobb ännu. Börja söka och spara jobb som intresserar dig!
        </Typography>
      </Box>
    );
  }

  // Group by status
  const groupedJobs = savedJobs.reduce(
    (acc, saved) => {
      const status = saved.status;
      if (!acc[status]) {
        acc[status] = [];
      }
      acc[status].push(saved);
      return acc;
    },
    {} as Record<SavedJobStatus, typeof savedJobs>
  );

  const statusOrder: SavedJobStatus[] = ['Applied', 'Interviewing', 'Offered', 'Saved', 'Accepted', 'Rejected', 'Withdrawn'];

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Sparade jobb ({savedJobs.length})
      </Typography>

      {statusOrder.map((status) => {
        const jobs = groupedJobs[status];
        if (!jobs || jobs.length === 0) return null;

        return (
          <Box key={status} sx={{ mb: 3 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
              <Chip
                label={savedStatusLabels[status]}
                color={savedStatusColors[status]}
                size="small"
              />
              <Typography variant="body2" color="text.secondary">
                ({jobs.length})
              </Typography>
            </Box>

            <Paper variant="outlined">
              <List disablePadding>
                {jobs.map((saved, index) => (
                  <ListItem
                    key={saved.id}
                    divider={index < jobs.length - 1}
                    sx={{ cursor: 'pointer' }}
                    onClick={() => onJobClick(saved.job.id)}
                  >
                    <ListItemText
                      primary={saved.job.title}
                      secondary={
                        <Box component="span" sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
                          <span>{saved.job.companyName}</span>
                          <Typography variant="caption" color="text.secondary">
                            Sparad {formatDate(saved.savedAt)}
                            {saved.appliedAt && ` • Ansökt ${formatDate(saved.appliedAt)}`}
                          </Typography>
                        </Box>
                      }
                    />
                    <ListItemSecondaryAction>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <FormControl size="small" sx={{ minWidth: 120 }}>
                          <Select
                            value={saved.status}
                            onChange={(e) => {
                              e.stopPropagation();
                              handleStatusChange(saved.job.id, e.target.value as SavedJobStatus);
                            }}
                            onClick={(e) => e.stopPropagation()}
                          >
                            {Object.entries(savedStatusLabels).map(([value, label]) => (
                              <MenuItem key={value} value={value}>
                                {label}
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                        <IconButton
                          size="small"
                          onClick={(e) => {
                            e.stopPropagation();
                            onJobClick(saved.job.id);
                          }}
                        >
                          <OpenInNewIcon fontSize="small" />
                        </IconButton>
                        <IconButton
                          size="small"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleRemove(saved.job.id);
                          }}
                          color="error"
                        >
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Box>
                    </ListItemSecondaryAction>
                  </ListItem>
                ))}
              </List>
            </Paper>
          </Box>
        );
      })}
    </Box>
  );
}
