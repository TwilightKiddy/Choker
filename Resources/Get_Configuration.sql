SELECT max_loudness, interval, mute_time
  FROM servers
  WHERE id = $id;