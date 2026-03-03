export const generateMezonState = (): string => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let state = '';
  for (let i = 0; i < 11; i++) {
    state += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return state;
};