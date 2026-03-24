import { useRef, useState } from 'react';
import {
  Box,
  Typography,
  Button,
  Card,
  CardContent,
  IconButton,
  Chip,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  ListItemSecondaryAction,
  CircularProgress,
  Alert,
} from '@mui/material';
import {
  CloudUpload as UploadIcon,
  Description as FileIcon,
  Delete as DeleteIcon,
  Star as StarIcon,
  StarBorder as StarBorderIcon,
  Download as DownloadIcon,
} from '@mui/icons-material';
import { useProfileStore } from '../../stores/profileStore';
import { profileApi } from '../../api/profile';

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

const formatDate = (dateString: string): string => {
  return new Date(dateString).toLocaleDateString('sv-SE', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
};

export function CVSection() {
  const { profile, uploadCV, deleteCV, setPrimaryCV, isLoading } = useProfileStore();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file type
    const allowedTypes = [
      'application/pdf',
      'application/msword',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    ];
    if (!allowedTypes.includes(file.type)) {
      setUploadError('Endast PDF och Word-dokument är tillåtna');
      return;
    }

    // Validate file size (max 10MB)
    if (file.size > 10 * 1024 * 1024) {
      setUploadError('Filen är för stor. Max 10 MB');
      return;
    }

    setUploadError(null);
    setUploading(true);

    try {
      await uploadCV(file);
    } catch (error) {
      setUploadError(error instanceof Error ? error.message : 'Kunde inte ladda upp filen');
    } finally {
      setUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleDelete = async (documentId: string) => {
    if (window.confirm('Är du säker på att du vill ta bort detta CV?')) {
      await deleteCV(documentId);
    }
  };

  const handleSetPrimary = async (documentId: string) => {
    await setPrimaryCV(documentId);
  };

  const handleDownload = (documentId: string) => {
    const url = profileApi.downloadCV(documentId);
    const token = localStorage.getItem('accessToken');

    // Create a temporary link with auth header
    fetch(url, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
      .then((response) => response.blob())
      .then((blob) => {
        const doc = profile?.cvDocuments.find((d) => d.id === documentId);
        const downloadUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.download = doc?.fileName || 'cv.pdf';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(downloadUrl);
      });
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">CV-dokument</Typography>
        <Button
          variant="outlined"
          startIcon={uploading ? <CircularProgress size={20} /> : <UploadIcon />}
          onClick={() => fileInputRef.current?.click()}
          disabled={isLoading || uploading}
        >
          {uploading ? 'Laddar upp...' : 'Ladda upp CV'}
        </Button>
        <input
          type="file"
          ref={fileInputRef}
          onChange={handleFileSelect}
          accept=".pdf,.doc,.docx"
          style={{ display: 'none' }}
        />
      </Box>

      {uploadError && (
        <Alert severity="error" onClose={() => setUploadError(null)} sx={{ mb: 2 }}>
          {uploadError}
        </Alert>
      )}

      {profile?.cvDocuments.length === 0 ? (
        <Card variant="outlined" sx={{ textAlign: 'center', py: 4 }}>
          <CardContent>
            <FileIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 1 }} />
            <Typography color="text.secondary">
              Inga CV-dokument uppladdade ännu.
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              Ladda upp ditt CV för att kunna matcha med jobb och låta arbetsgivare hitta dig.
            </Typography>
          </CardContent>
        </Card>
      ) : (
        <List>
          {profile?.cvDocuments.map((doc) => (
            <ListItem
              key={doc.id}
              sx={{
                border: '1px solid',
                borderColor: doc.isPrimary ? 'primary.main' : 'divider',
                borderRadius: 1,
                mb: 1,
                bgcolor: doc.isPrimary ? 'primary.light' : 'background.paper',
              }}
            >
              <ListItemIcon>
                <FileIcon color={doc.isPrimary ? 'primary' : 'action'} />
              </ListItemIcon>
              <ListItemText
                primary={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    {doc.fileName}
                    {doc.isPrimary && (
                      <Chip label="Primärt" size="small" color="primary" />
                    )}
                    {doc.isParsed && (
                      <Chip label="Analyserat" size="small" color="success" variant="outlined" />
                    )}
                  </Box>
                }
                secondary={`${formatFileSize(doc.fileSize)} - Uppladdad ${formatDate(doc.uploadedAt)}`}
              />
              <ListItemSecondaryAction>
                <IconButton
                  onClick={() => handleSetPrimary(doc.id)}
                  disabled={isLoading || doc.isPrimary}
                  title={doc.isPrimary ? 'Redan primärt' : 'Sätt som primärt'}
                  aria-label={doc.isPrimary ? `${doc.fileName} ar redan primart` : `Satt ${doc.fileName} som primart`}
                >
                  {doc.isPrimary ? <StarIcon color="primary" /> : <StarBorderIcon />}
                </IconButton>
                <IconButton
                  onClick={() => handleDownload(doc.id)}
                  disabled={isLoading}
                  title="Ladda ner"
                  aria-label={`Ladda ner ${doc.fileName}`}
                >
                  <DownloadIcon />
                </IconButton>
                <IconButton
                  onClick={() => handleDelete(doc.id)}
                  disabled={isLoading}
                  title="Ta bort"
                  aria-label={`Ta bort ${doc.fileName}`}
                >
                  <DeleteIcon />
                </IconButton>
              </ListItemSecondaryAction>
            </ListItem>
          ))}
        </List>
      )}

      <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
        Accepterade format: PDF, DOC, DOCX (max 10 MB)
      </Typography>
    </Box>
  );
}
