class HttpError extends Error {
  constructor(public code: number, public message: string) {
    super(message);
    this.name = 'HttpError';
    Object.setPrototypeOf(this, HttpError.prototype);
  }
}

export default HttpError;
