export const getMezonCallbackParams = (urlSearchParams: URLSearchParams) => {
  return {
    code: urlSearchParams.get('code'),
    state: urlSearchParams.get('state'),
  };
};
