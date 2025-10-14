import HttpError from './HttpError';

class ServerError extends HttpError {
  constructor(message: string) {
    super(500, message);
  }
}

export default ServerError;
