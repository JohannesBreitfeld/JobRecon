import { createTheme, alpha } from '@mui/material/styles';

// Brand palette - derived from logo colors
const colors = {
  navy: '#0d3b66',
  blue: '#1565a0',
  green: '#5ba532',
  greenDark: '#4a8a29',
  greenLight: '#6ec43e',
  gray: '#64748b',
  lightGray: '#e2e8f0',
  bgDefault: '#f8fafc',
} as const;

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: colors.blue,
      dark: colors.navy,
      light: '#2196f3',
      contrastText: '#ffffff',
    },
    secondary: {
      main: colors.green,
      dark: colors.greenDark,
      light: colors.greenLight,
      contrastText: '#ffffff',
    },
    success: {
      main: colors.green,
      dark: colors.greenDark,
      light: colors.greenLight,
    },
    background: {
      default: colors.bgDefault,
      paper: '#ffffff',
    },
    divider: colors.lightGray,
    text: {
      primary: colors.navy,
      secondary: colors.gray,
    },
  },
  shape: {
    borderRadius: 8,
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h1: {
      fontWeight: 700,
      letterSpacing: '-0.02em',
    },
    h2: {
      fontWeight: 700,
      letterSpacing: '-0.01em',
    },
    h3: {
      fontWeight: 600,
    },
    h4: {
      fontWeight: 600,
    },
    h5: {
      fontWeight: 600,
    },
    h6: {
      fontWeight: 600,
    },
    button: {
      fontWeight: 600,
    },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          borderRadius: 8,
          padding: '8px 20px',
        },
        containedSecondary: {
          '&:hover': {
            backgroundColor: colors.greenDark,
          },
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundColor: colors.navy,
          boxShadow: '0 1px 3px rgba(0,0,0,0.12)',
          borderRadius: 0,
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          borderRadius: 12,
        },
        elevation1: {
          boxShadow: '0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.06)',
        },
        elevation3: {
          boxShadow: '0 4px 12px rgba(0,0,0,0.08), 0 2px 4px rgba(0,0,0,0.04)',
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          transition: 'box-shadow 0.2s ease, transform 0.2s ease',
          '&:hover': {
            boxShadow: '0 8px 24px rgba(0,0,0,0.1)',
          },
        },
      },
    },
    MuiTextField: {
      defaultProps: {
        variant: 'outlined',
      },
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: 8,
          },
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: 8,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 500,
        },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 500,
          fontSize: '0.95rem',
        },
      },
    },
    MuiTabs: {
      styleOverrides: {
        indicator: {
          backgroundColor: colors.green,
          height: 3,
          borderRadius: '3px 3px 0 0',
        },
      },
    },
    MuiAlert: {
      styleOverrides: {
        root: {
          borderRadius: 8,
        },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          borderRadius: 16,
        },
      },
    },
    MuiTooltip: {
      styleOverrides: {
        tooltip: {
          borderRadius: 6,
        },
      },
    },
    MuiLinearProgress: {
      styleOverrides: {
        root: {
          borderRadius: 4,
          backgroundColor: alpha(colors.green, 0.15),
        },
        bar: {
          borderRadius: 4,
          backgroundColor: colors.green,
        },
      },
    },
  },
});
