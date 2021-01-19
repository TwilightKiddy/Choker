INSERT INTO servers (id, mute_time)
  VALUES($id, $mute_time)
  ON CONFLICT(id)
  DO UPDATE SET mute_time=excluded.mute_time;