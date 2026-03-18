import { createTheme } from '@mui/material/styles';

// Color palette
const colors = {
  navy: '#091057',
  blue: '#024CAA',
  orange: '#EC8305',
  lightGray: '#DBD3D3',
} as const;

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: colors.blue,
      dark: colors.navy,
      contrastText: '#ffffff',
    },
    secondary: {
      main: colors.orange,
      contrastText: '#ffffff',
    },
    background: {
      default: '#fafafa',
      paper: '#ffffff',
    },
    divider: colors.lightGray,
    text: {
      primary: colors.navy,
      secondary: '#5a5a5a',
    },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundColor: colors.navy,
        },
      },
    },
  },
});
