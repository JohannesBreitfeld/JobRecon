import { useState } from 'react';
import { Container, Box, Tabs, Tab, Typography, Paper, Badge } from '@mui/material';
import { Search as SearchIcon, Bookmark as BookmarkIcon } from '@mui/icons-material';
import { JobSearchFilters } from './JobSearchFilters';
import { JobList } from './JobList';
import { SavedJobsList } from './SavedJobsList';
import { JobDetailsDialog } from './JobDetailsDialog';
import { useSavedJobs } from '../../api/hooks/useJobs';
import { useAuthStore } from '../../stores/authStore';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div role="tabpanel" hidden={value !== index} {...other}>
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
}

export function JobsPage() {
  const [tabValue, setTabValue] = useState(0);
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const { isAuthenticated } = useAuthStore();
  const { data: savedJobs } = useSavedJobs();
  const savedCount = isAuthenticated ? (savedJobs?.length ?? 0) : 0;

  const handleJobClick = (jobId: string) => {
    setSelectedJobId(jobId);
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setSelectedJobId(null);
  };

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ mb: 3 }}>
        <Typography variant="h4" gutterBottom>
          Jobb
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Sök bland lediga tjänster eller bläddra bland dina sparade jobb.
        </Typography>
      </Box>

      <Paper elevation={0} sx={{ mb: 3, border: 1, borderColor: 'divider' }}>
        <Tabs
          value={tabValue}
          onChange={(_, newValue) => setTabValue(newValue)}
          variant="fullWidth"
        >
          <Tab icon={<SearchIcon />} label="Sök jobb" iconPosition="start" />
          <Tab
            icon={
              savedCount > 0 ? (
                <Badge badgeContent={savedCount} color="secondary" max={99}>
                  <BookmarkIcon />
                </Badge>
              ) : (
                <BookmarkIcon />
              )
            }
            label="Sparade jobb"
            iconPosition="start"
          />
        </Tabs>
      </Paper>

      <TabPanel value={tabValue} index={0}>
        <JobSearchFilters />
        <JobList onJobClick={handleJobClick} />
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <SavedJobsList onJobClick={handleJobClick} />
      </TabPanel>

      <JobDetailsDialog
        jobId={selectedJobId}
        open={dialogOpen}
        onClose={handleCloseDialog}
      />
    </Container>
  );
}
