import { health, ok2xx } from './lib.js';

export const options = {
  scenarios: {
    rps: {
      executor: 'constant-arrival-rate',
      rate: Number(__ENV.RATE || 50),
      timeUnit: '1s',
      duration: __ENV.DURATION || '15s',
      preAllocatedVUs: Number(__ENV.PRE_VUS || 20),
      maxVUs: Number(__ENV.MAX_VUS || 80),
    },
  },
};

export default function () {
  ok2xx(health());
}
