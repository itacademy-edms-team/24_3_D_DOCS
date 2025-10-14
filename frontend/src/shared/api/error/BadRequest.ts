import HttpError from './HttpError';

class BadRequest extends HttpError {
  constructor(message: string) {
    super(400, message);
  }
}

export default BadRequest;
