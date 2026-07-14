{{/*
Expand the name of the chart.
*/}}
{{- define "ego.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
Truncated to 63 characters because some Kubernetes name fields have that limit.
*/}}
{{- define "ego.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart label.
*/}}
{{- define "ego.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels.
*/}}
{{- define "ego.labels" -}}
helm.sh/chart: {{ include "ego.chart" . }}
{{ include "ego.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels.
*/}}
{{- define "ego.selectorLabels" -}}
app.kubernetes.io/name: {{ include "ego.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account.
*/}}
{{- define "ego.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "ego.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Name of the in-cluster PostgreSQL Service (used to auto-derive the connection string).
*/}}
{{- define "ego.postgresServiceName" -}}
{{- printf "%s-postgres" (include "ego.fullname" .) }}
{{- end }}

{{/*
Resolve the effective database connection string.
Priority:
  1. secrets.connectionString (explicit override via --set at deploy time)
  2. Auto-derived from postgresql.* values when postgresql.enabled is true.
     Pass the password via --set at deploy time; never commit a real value to git.
  3. Fallback placeholder.
*/}}
{{- define "ego.connectionString" -}}
{{- if .Values.secrets.connectionString }}
{{- .Values.secrets.connectionString }}
{{- else if .Values.postgresql.enabled }}
{{- printf "Host=%s;Database=%s;Username=%s;Password=%s" (include "ego.postgresServiceName" .) .Values.postgresql.database .Values.postgresql.username .Values.postgresql.password }}
{{- else }}
CHANGE_THIS_CONNECTION_STRING_BEFORE_DEPLOYING
{{- end }}
{{- end }}
