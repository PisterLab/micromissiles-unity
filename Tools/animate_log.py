"""Generates an HTML file containing an interactive 3D animation of the
telemetry file.
"""

import json
from pathlib import Path
from typing import Any

import pandas as pd
import unity
import utils
from absl import app, flags, logging
from constants import Column, is_interceptor, is_threat

FLAGS = flags.FLAGS

# Helper columns in the JSON object.
IS_INTERCEPTOR = "IsInterceptor"
IS_THREAT = "IsThreat"


def _generate_agents_data(
        telemetry_df: pd.DataFrame) -> dict[str, dict[str, Any]]:
    """Generates a dictionary containing the data for each agent.

    Args:
        telemetry_df: Dataframe containing the telemetry data.

    Returns:
        A dictionary mapping from the agent ID to another dictionary containing
        the corresponding agent data.
    """
    all_agents_data: dict[str, dict[str, Any]] = {}
    for agent_id, agent_data in telemetry_df.groupby(Column.AGENT_ID):
        agent_type = agent_data[Column.AGENT_TYPE].iloc[0]
        agent_properties = {
            IS_INTERCEPTOR: is_interceptor(agent_type),
            IS_THREAT: is_threat(agent_type),
        }
        agent_trajectory = agent_data[[
            Column.TIME,
            Column.POSITION_X,
            Column.POSITION_Y,
            Column.POSITION_Z,
        ]].to_dict(orient="list")
        all_agents_data[agent_id] = agent_trajectory | agent_properties
    return all_agents_data


def generate_animation(
    telemetry_df: pd.DataFrame,
    telemetry_file_path: Path,
    output: str,
    fps: float,
) -> None:
    """Generates an HTML file containing an interactive 3D animation of the
    telemetry data.

    Args:
        telemetry_df: Dataframe containing the telemetry data.
        telemetry_file_path: Telemetry file path.
        output: Output HTML file.
        fps: Target frame rate.
    """
    min_time = telemetry_df[Column.TIME].min()
    max_time = telemetry_df[Column.TIME].max()
    all_agents_data = _generate_agents_data(telemetry_df)
    html = f"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8" />
    <title>{telemetry_file_path.stem}</title>
    <script src="https://cdn.plot.ly/plotly-3.3.0.min.js"></script>
</head>

<body>
    <div id="plot" style="width: 100%; height: 95vh;"></div>
    <script>
const agents = {json.dumps(all_agents_data, separators=(",", ":"))};
const minTime = {min_time};
const maxTime = {max_time};
const fps = {fps};
const restartDelayMs = 2000;

const timeColumn = "{Column.TIME}";
const positionXColumn = "{Column.POSITION_X}";
const positionYColumn = "{Column.POSITION_Y}";
const positionZColumn = "{Column.POSITION_Z}";

function firstGreaterThan(arr, N) {{
  let left = 0;
  let right = arr.length;

  while (left < right) {{
    const mid = (left + right) >> 1;
    if (arr[mid] > N) {{
      right = mid;
    }} else {{
      left = mid + 1;
    }}
  }}
  return left;
}}

function interpolate(times, values, t) {{
    if (t < times[0]) {{
        return null;
    }}
    if (t >= times[times.length - 1]) {{
        return values[values.length - 1];
    }}

    // Binary search through the times array.
    let left = 0;
    let right = times.length - 1;
    while (right - left > 1) {{
        const mid = (left + right) >> 1;
        if (times[mid] < t) {{
            left = mid;
        }}
        else {{
            right = mid;
        }}
    }}

    const t0 = times[left];
    const t1 = times[right];
    const v0 = values[left];
    const v1 = values[right];
    // Use linear interpolation.
    return v0 + (v1 - v0) * ((t - t0) / (t1 - t0));
}}

function color(data) {{
    if (data.{IS_INTERCEPTOR}) {{
        return "blue";
    }}
    if (data.{IS_THREAT}) {{
        return "red";
    }}
    return "black";
}}

function symbol(data) {{
    if (data.{IS_INTERCEPTOR}) {{
        return "diamond";
    }}
    if (data.{IS_THREAT}) {{
        return "square";
    }}
    return "circle";
}}

let data = [];
let agentToTrajectoryIndex = {{}};
let agentToMarkerIndex = {{}};
for (const agent in agents) {{
    const agentData = agents[agent];

    // Agent trajectory.
    agentToTrajectoryIndex[agent] = data.length;
    data.push({{
        name: agent,
        type: "scatter3d",
        mode: "lines",
        x: [],
        y: [],
        z: [],
        line: {{
            color: color(agentData),
            width: 2,
        }},
        showlegend: false,
    }});

    // Agent marker.
    agentToMarkerIndex[agent] = data.length;
    data.push({{
        name: agent,
        type: "scatter3d",
        mode: "markers",
        x: [],
        y: [],
        z: [],
        marker: {{
            color: color(agentData),
            size: 4,
            symbol: symbol(agentData),
        }},
        showlegend: false,
    }});
}}

const layout = {{
    title: {{
        text: "Time: 0.00",
        subtitle: {{
            text: "{telemetry_file_path}",
        }},
    }},
    font: {{
        family: "Helvetica Neue, Nimbus Sans, Arial, sans-serif",
    }},
    scene: {{
        aspectmode: "data",
        camera : {{
            eye: {{ x: 3, y: 0, z: 1.5 }},
            up: {{ x: 0, y: 0, z: 1 }},
            projection: "orthographic",
        }},
        xaxis: {{
            title: {{ text: "x [m]" }},
        }},
        yaxis: {{
            title: {{ text: "z [m]" }},
        }},
        zaxis: {{
            title: {{ text: "y [m]" }},
        }},
    }},
}};

Plotly.newPlot("plot", data, layout);

let t = minTime;
function animate() {{
    t += 1.0 / fps;
    if (t > maxTime) {{
        setTimeout(() => reset(), restartDelayMs);
        return;
    }}

    const dataUpdate = {{
        x: data.map(trace => trace.x),
        y: data.map(trace => trace.y),
        z: data.map(trace => trace.z),
    }};
    for (const agent in agents) {{
        const agentData = agents[agent];
        const trajectoryIndex = agentToTrajectoryIndex[agent];
        const markerIndex = agentToMarkerIndex[agent];

        // Swap the y and z coordinates for plotting to conform to Unity's conventions.
        const trajectoryX = dataUpdate.x[trajectoryIndex];
        const trajectoryY = dataUpdate.z[trajectoryIndex];
        const trajectoryZ = dataUpdate.y[trajectoryIndex];
        const markerX = dataUpdate.x[markerIndex];
        const markerY = dataUpdate.z[markerIndex];
        const markerZ = dataUpdate.y[markerIndex];

        const currentTimeIndex = firstGreaterThan(agentData[timeColumn], t);
        if (currentTimeIndex !== 0) {{
            trajectoryX.pop();
            trajectoryY.pop();
            trajectoryZ.pop();

            // Add new positions to the trajectory.
            if (trajectoryX.length < currentTimeIndex) {{
                trajectoryX.push(...agentData[positionXColumn].slice(trajectoryX.length, currentTimeIndex));
            }}
            if (trajectoryY.length < currentTimeIndex) {{
                trajectoryY.push(...agentData[positionYColumn].slice(trajectoryY.length, currentTimeIndex));
            }}
            if (trajectoryZ.length < currentTimeIndex) {{
                trajectoryZ.push(...agentData[positionZColumn].slice(trajectoryZ.length, currentTimeIndex));
            }}

            // Determine the current position.
            const currentX = interpolate(agentData[timeColumn], agentData[positionXColumn], t);
            trajectoryX.push(currentX);
            markerX[0] = currentX;
            const currentY = interpolate(agentData[timeColumn], agentData[positionYColumn], t);
            trajectoryY.push(currentY);
            markerY[0] = currentY;
            const currentZ = interpolate(agentData[timeColumn], agentData[positionZColumn], t);
            trajectoryZ.push(currentZ);
            markerZ[0] = currentZ;
        }}
    }}
    const layoutUpdate = {{
        "title.text": `Time: ${{t.toFixed(2)}}`,
    }};

    Plotly.update("plot", dataUpdate, layoutUpdate);
    setTimeout(() => requestAnimationFrame(animate), 1000.0 / fps);
}}

function reset() {{
    t = minTime;
    const dataUpdate = {{
        x: Array.from({{ length: data.length }}, () => []),
        y: Array.from({{ length: data.length }}, () => []),
        z: Array.from({{ length: data.length }}, () => []),
    }};
    const layoutUpdate = {{
        "title.text": "Time: 0.00",
    }};
    Plotly.update("plot", dataUpdate, layoutUpdate);
    requestAnimationFrame(animate);
}}

requestAnimationFrame(animate);
    </script>
</body>
</html>
"""

    with open(output, "w") as f:
        f.write(html)
    logging.info("Successfully generated HTML file: %s.", output)


def main(argv):
    assert len(argv) == 1, argv

    if FLAGS.telemetry_file:
        telemetry_file_path = Path(FLAGS.telemetry_file)
    else:
        telemetry_file_path = utils.find_latest_telemetry_file(
            FLAGS.log_search_dir)
    if not telemetry_file_path:
        raise ValueError("No telemetry file was provided.")

    telemetry_df = utils.read_telemetry_file(telemetry_file_path)
    generate_animation(
        telemetry_df,
        telemetry_file_path,
        FLAGS.output,
        FLAGS.fps,
    )


if __name__ == "__main__":
    flags.DEFINE_string("telemetry_file", None,
                        "Path to the telemetry CSV file.")
    flags.DEFINE_string("log_search_dir", unity.get_persistent_data_directory(),
                        "Log directory in which to search for logs.")
    flags.DEFINE_string("output", None, "Output HTML file.")
    flags.DEFINE_float("fps", 30, "Target frame rate.")
    flags.mark_flag_as_required("output")

    app.run(main)
