import {
  Card,
  CardContent,
  CardActionArea,
  Typography,
  Box,
  Chip,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Bookmark as BookmarkIcon,
  BookmarkBorder as BookmarkBorderIcon,
  LocationOn as LocationIcon,
  AccessTime as TimeIcon,
  Work as WorkIcon,
} from '@mui/icons-material';
import { useSaveJob, useRemoveSavedJob } from '../../api/hooks/useJobs';
import type { JobListResponse, WorkLocationType, EmploymentType, SavedJobStatus } from '../../api/jobs';

const workLocationLabels: Record<WorkLocationType, string> = {
  OnSite: 'På plats',
  Remote: 'Distans',
  Hybrid: 'Hybrid',
};

const employmentTypeLabels: Record<EmploymentType, string> = {
  FullTime: 'Heltid',
  PartTime: 'Deltid',
  Contract: 'Kontrakt',
  Freelance: 'Frilans',
  Internship: 'Praktik',
  Temporary: 'Tidsbegränsad',
};

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

interface JobCardProps {
  job: JobListResponse;
  onClick: () => void;
}

export function JobCard({ job, onClick }: JobCardProps) {
  const saveJobMutation = useSaveJob();
  const removeSavedJobMutation = useRemoveSavedJob();
  const isLoading = saveJobMutation.isPending || removeSavedJobMutation.isPending;

  const handleSaveClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (job.isSaved) {
      removeSavedJobMutation.mutate(job.id);
    } else {
      saveJobMutation.mutate({ jobId: job.id });
    }
  };

  const formatSalary = () => {
    if (!job.salaryMin && !job.salaryMax) return null;
    const currency = job.salaryCurrency || 'SEK';
    if (job.salaryMin && job.salaryMax) {
      return `${job.salaryMin.toLocaleString()} - ${job.salaryMax.toLocaleString()} ${currency}`;
    }
    if (job.salaryMin) {
      return `Från ${job.salaryMin.toLocaleString()} ${currency}`;
    }
    return `Upp till ${job.salaryMax?.toLocaleString()} ${currency}`;
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    const now = new Date();
    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Idag';
    if (diffDays === 1) return 'Igår';
    if (diffDays < 7) return `${diffDays} dagar sedan`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} veckor sedan`;
    return date.toLocaleDateString('sv-SE');
  };

  const salary = formatSalary();

  return (
    <Card variant="outlined" sx={{ height: '100%' }}>
      <CardActionArea onClick={onClick} sx={{ height: '100%' }}>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
              {job.companyLogoUrl ? (
                <Box
                  component="img"
                  src={job.companyLogoUrl}
                  alt={job.companyName}
                  sx={{ width: 40, height: 40, objectFit: 'contain', borderRadius: 1 }}
                />
              ) : (
                <Box
                  sx={{
                    width: 40,
                    height: 40,
                    bgcolor: 'primary.light',
                    borderRadius: 1,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: 'primary.contrastText',
                    fontWeight: 'bold',
                  }}
                >
                  {job.companyName.charAt(0).toUpperCase()}
                </Box>
              )}
              <Box>
                <Typography variant="subtitle1" fontWeight="medium" sx={{ lineHeight: 1.2 }}>
                  {job.title}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {job.companyName}
                </Typography>
              </Box>
            </Box>

            <Tooltip title={job.isSaved ? 'Ta bort sparad' : 'Spara jobb'}>
              <IconButton
                size="small"
                onClick={handleSaveClick}
                disabled={isLoading}
                color={job.isSaved ? 'primary' : 'default'}
              >
                {job.isSaved ? <BookmarkIcon /> : <BookmarkBorderIcon />}
              </IconButton>
            </Tooltip>
          </Box>

          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 1.5 }}>
            {job.location && (
              <Chip
                icon={<LocationIcon fontSize="small" />}
                label={job.location}
                size="small"
                variant="outlined"
              />
            )}
            {job.workLocationType && (
              <Chip
                label={workLocationLabels[job.workLocationType]}
                size="small"
                color={job.workLocationType === 'Remote' ? 'success' : 'default'}
                variant="outlined"
              />
            )}
            {job.employmentType && (
              <Chip
                icon={<WorkIcon fontSize="small" />}
                label={employmentTypeLabels[job.employmentType]}
                size="small"
                variant="outlined"
              />
            )}
          </Box>

          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              {job.postedAt && (
                <>
                  <TimeIcon fontSize="small" color="action" />
                  <Typography variant="caption" color="text.secondary">
                    {formatDate(job.postedAt)}
                  </Typography>
                </>
              )}
            </Box>

            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              {salary && (
                <Typography variant="body2" color="primary" fontWeight="medium">
                  {salary}
                </Typography>
              )}
              {job.savedStatus && (
                <Chip
                  label={savedStatusLabels[job.savedStatus]}
                  size="small"
                  color={savedStatusColors[job.savedStatus]}
                />
              )}
            </Box>
          </Box>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
