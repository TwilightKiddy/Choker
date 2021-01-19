INSERT INTO servers (id, interval)
  VALUES($id, $interval)
  ON CONFLICT(id)
  DO UPDATE SET interval=excluded.interval;